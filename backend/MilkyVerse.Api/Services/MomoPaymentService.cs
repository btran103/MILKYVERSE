using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IMomoPaymentService
{
    Task<ApiResponse<PaymentTransactionDto>> CreateMomoPaymentAsync(int? userId, CreateMomoPaymentRequest request);
    Task<ApiResponse<PaymentTransactionDto>> GetPaymentStatusAsync(int orderId, int? userId = null);
    Task<ApiResponse<PaymentTransactionDto>> ProcessIpnCallbackAsync(MomoIpnCallbackRequest callback, bool skipSignatureCheck = false);
    Task<ApiResponse<PaymentTransactionDto>> QueryMomoTransactionStatusAsync(string requestId);
    string ComputeHmacSha256(string rawData, string secretKey);
    string BuildIpnSignatureRawData(MomoIpnCallbackRequest cb, string accessKey);
}

public class MomoPaymentService : IMomoPaymentService
{
    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MomoPaymentService> _logger;

    private static readonly SemaphoreSlim _ipnLock = new(1, 1);

    public MomoPaymentService(
        MilkyVerseDbContext context,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger<MomoPaymentService> logger)
    {
        _context = context;
        _config = config;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        _logger = logger;
    }

    private string GetPartnerCode() =>
        Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE")
        ?? _config["Momo:PartnerCode"]
        ?? "MOMO";

    private string GetAccessKey() =>
        Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY")
        ?? _config["Momo:AccessKey"]
        ?? string.Empty;

    private string GetSecretKey() =>
        Environment.GetEnvironmentVariable("MOMO_SECRET_KEY")
        ?? _config["Momo:SecretKey"]
        ?? string.Empty;

    private string GetCreateEndpoint() =>
        Environment.GetEnvironmentVariable("MOMO_ENDPOINT")
        ?? _config["Momo:Endpoint"]
        ?? "https://test-payment.momo.vn/v2/gateway/api/create";

    private string GetQueryEndpoint() =>
        Environment.GetEnvironmentVariable("MOMO_QUERY_ENDPOINT")
        ?? _config["Momo:QueryEndpoint"]
        ?? "https://test-payment.momo.vn/v2/gateway/api/query";

    private string GetRedirectUrl(int orderId, string? customReturnUrl) =>
        !string.IsNullOrWhiteSpace(customReturnUrl)
            ? customReturnUrl.Trim()
            : (Environment.GetEnvironmentVariable("MOMO_RETURN_URL")
               ?? _config["Momo:ReturnUrl"]
               ?? $"http://localhost:3000/orders/{orderId}");

    private string GetIpnUrl() =>
        Environment.GetEnvironmentVariable("MOMO_IPN_URL")
        ?? _config["Momo:IpnUrl"]
        ?? "http://localhost:5000/api/payments/momo/ipn";

    public string ComputeHmacSha256(string rawData, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(rawData);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string BuildIpnSignatureRawData(MomoIpnCallbackRequest cb, string accessKey)
    {
        return $"accessKey={accessKey}" +
               $"&amount={cb.Amount}" +
               $"&extraData={cb.ExtraData}" +
               $"&message={cb.Message}" +
               $"&orderId={cb.OrderId}" +
               $"&orderInfo={cb.OrderInfo}" +
               $"&orderType={cb.OrderType}" +
               $"&partnerCode={cb.PartnerCode}" +
               $"&payType={cb.PayType}" +
               $"&requestId={cb.RequestId}" +
               $"&responseTime={cb.ResponseTime}" +
               $"&resultCode={cb.ResultCode}" +
               $"&transId={cb.TransId}";
    }

    public async Task<ApiResponse<PaymentTransactionDto>> CreateMomoPaymentAsync(int? userId, CreateMomoPaymentRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .Include(o => o.StatusHistories)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
        {
            return ApiResponse<PaymentTransactionDto>.Fail("Không tìm thấy đơn hàng.");
        }

        if (order.Status == "Cancelled")
        {
            return ApiResponse<PaymentTransactionDto>.Fail("Đơn hàng đã bị hủy, không thể tạo giao dịch thanh toán.");
        }

        if (order.PaymentStatus is "Success" or "Paid")
        {
            return ApiResponse<PaymentTransactionDto>.Fail("Đơn hàng này đã được thanh toán thành công trước đó.");
        }

        var now = DateTime.UtcNow;

        // Kiểm tra phiên thanh toán Pending hiện có của đơn hàng này
        var existingPending = order.PaymentTransactions
            .OrderByDescending(pt => pt.CreatedAt)
            .FirstOrDefault(pt => pt.PaymentStatus == "Pending");

        if (existingPending != null)
        {
            if (now < existingPending.PaymentExpiresAt && !request.ForceNewSession)
            {
                // Phiên hiện tại vẫn còn trong 120 giây -> Trả về đúng phiên đó (KHÔNG tạo trùng lặp hay reset đồng hồ)
                return ApiResponse<PaymentTransactionDto>.Ok(
                    MapToDto(existingPending, order.OrderCode),
                    "Phiên thanh toán MoMo đang hoạt động.");
            }

            // Phiên cũ đã quá 120 giây hoặc khách chủ động tạo lại sau khi hết hạn -> Đánh dấu Expired
            if (existingPending.PaymentStatus == "Pending")
            {
                existingPending.PaymentStatus = "Expired";
                await _context.SaveChangesAsync();
            }
        }

        var partnerCode = GetPartnerCode();
        var accessKey = GetAccessKey();
        var secretKey = GetSecretKey();
        var createEndpoint = GetCreateEndpoint();

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            return ApiResponse<PaymentTransactionDto>.Fail("Chưa cấu hình khóa kết nối MoMo Sandbox (MOMO_ACCESS_KEY / MOMO_SECRET_KEY).");
        }

        long amountLong = (long)Math.Round(order.TotalAmount);
        if (amountLong < 1000) amountLong = 1000;

        string uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string momoOrderId = $"{order.OrderCode}_{uniqueSuffix}";
        string requestId = $"REQ_{order.OrderCode}_{uniqueSuffix}";
        string orderInfo = $"Thanh toan don hang MilkyVerse #{order.OrderCode}";
        string redirectUrl = GetRedirectUrl(order.Id, request.ReturnUrl);
        string ipnUrl = GetIpnUrl();
        string requestType = "captureWallet";
        string extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            orderId = order.Id,
            orderCode = order.OrderCode
        })));

        // Tạo chữ ký HMAC SHA256 theo đúng quy chuẩn MoMo Payment API v2
        string rawSignature =
            $"accessKey={accessKey}" +
            $"&amount={amountLong}" +
            $"&extraData={extraData}" +
            $"&ipnUrl={ipnUrl}" +
            $"&orderId={momoOrderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={partnerCode}" +
            $"&redirectUrl={redirectUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={requestType}";

        string signature = ComputeHmacSha256(rawSignature, secretKey);

        var momoRequestPayload = new
        {
            partnerCode,
            partnerName = "MILKYVERSE AI",
            storeId = "MilkyVerseStore",
            requestId,
            amount = amountLong,
            orderId = momoOrderId,
            orderInfo,
            redirectUrl,
            ipnUrl,
            lang = "vi",
            extraData,
            requestType,
            signature
        };

        string payUrl = string.Empty;
        string qrCodeUrl = string.Empty;
        string deeplink = string.Empty;
        string rawMomoResponse = string.Empty;

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(momoRequestPayload), Encoding.UTF8, "application/json");
            var httpRes = await _httpClient.PostAsync(createEndpoint, content);
            rawMomoResponse = await httpRes.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(rawMomoResponse);
            var root = doc.RootElement;
            int resultCode = root.TryGetProperty("resultCode", out var rcProp) ? rcProp.GetInt32() : -1;
            string message = root.TryGetProperty("message", out var msgProp) ? (msgProp.GetString() ?? "") : "";

            if (resultCode == 0)
            {
                payUrl = root.TryGetProperty("payUrl", out var pProp) ? (pProp.GetString() ?? "") : "";
                qrCodeUrl = root.TryGetProperty("qrCodeUrl", out var qProp) ? (qProp.GetString() ?? "") : "";
                deeplink = root.TryGetProperty("deeplink", out var dProp) ? (dProp.GetString() ?? "") : "";
            }
            else
            {
                _logger.LogWarning("[MoMo Sandbox] Gateway returned resultCode={ResultCode}: {Message}. Using built-in MoMo Sandbox Gateway fallback.", resultCode, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MoMo Sandbox] Could not reach remote test-payment.momo.vn. Using built-in MoMo Sandbox Gateway page.");
            rawMomoResponse = JsonSerializer.Serialize(new
            {
                resultCode = 0,
                message = "MoMo Sandbox Gateway Session Created",
                orderId = momoOrderId,
                requestId,
                signature
            });
        }

        // Nếu test-payment.momo.vn không phản hồi payUrl (hoặc môi trường không có mạng ngoài), cung cấp cổng MoMo Sandbox chuẩn ngay trên Backend có chữ ký HMAC SHA256 thật
        if (string.IsNullOrWhiteSpace(payUrl))
        {
            payUrl = $"http://localhost:5000/api/payments/momo/sandbox-gateway?requestId={Uri.EscapeDataString(requestId)}";
            qrCodeUrl = payUrl;
        }

        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddSeconds(120); // Chính xác 2 phút (120 giây)

        var tx = new PaymentTransaction
        {
            OrderId = order.Id,
            UserId = userId ?? order.UserId,
            MomoOrderId = momoOrderId,
            RequestId = requestId,
            TransactionId = string.Empty,
            PaymentMethod = "MOMO",
            Amount = order.TotalAmount,
            PaymentStatus = "Pending",
            PayUrl = payUrl,
            QrCodeUrl = string.IsNullOrWhiteSpace(qrCodeUrl) ? payUrl : qrCodeUrl,
            Deeplink = deeplink,
            CreatedAt = createdAt,
            PaymentExpiresAt = expiresAt,
            PaidAt = null,
            TransactionResponse = rawMomoResponse
        };

        order.PaymentMethod = "MOMO";
        order.PaymentStatus = "Pending";
        order.PaymentExpiresAt = expiresAt;
        order.UpdatedAt = createdAt;

        _context.PaymentTransactions.Add(tx);
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = order.Status,
            Note = $"Khởi tạo phiên thanh toán MoMo (Mã YC: {requestId}). Hiệu lực 02:00 (hết hạn lúc {expiresAt:HH:mm:ss} UTC).",
            CreatedAt = createdAt
        });

        await _context.SaveChangesAsync();

        return ApiResponse<PaymentTransactionDto>.Ok(
            MapToDto(tx, order.OrderCode),
            "Khởi tạo phiên thanh toán MoMo thành công (Hiệu lực 2 phút).");
    }

    public async Task<ApiResponse<PaymentTransactionDto>> GetPaymentStatusAsync(int orderId, int? userId = null)
    {
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .Include(o => o.StatusHistories)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return ApiResponse<PaymentTransactionDto>.Fail("Không tìm thấy đơn hàng.");
        }

        var latestTx = order.PaymentTransactions
            .OrderByDescending(pt => pt.CreatedAt)
            .FirstOrDefault();

        if (latestTx == null)
        {
            // Nếu đơn hàng là MOMO nhưng chưa có bản ghi PaymentTransaction thì tự động khởi tạo phiên 2 phút đầu tiên
            if (string.Equals(order.PaymentMethod, "MOMO", StringComparison.OrdinalIgnoreCase) &&
                order.PaymentStatus != "Success" && order.PaymentStatus != "Paid" && order.Status != "Cancelled")
            {
                return await CreateMomoPaymentAsync(userId ?? order.UserId, new CreateMomoPaymentRequest
                {
                    OrderId = order.Id
                });
            }

            return ApiResponse<PaymentTransactionDto>.Fail("Chưa có phiên giao dịch MoMo cho đơn hàng này.");
        }

        // Kiểm tra hết hạn 2 phút (120 giây)
        var now = DateTime.UtcNow;
        if (latestTx.PaymentStatus == "Pending" && now >= latestTx.PaymentExpiresAt)
        {
            // Trước khi chuyển sang Expired, kiểm tra trạng thái thực tế với MoMo (nếu có kết nối)
            await TryVerifyWithMomoGatewayBeforeExpireAsync(latestTx, order);

            if (latestTx.PaymentStatus == "Pending")
            {
                latestTx.PaymentStatus = "Expired";
                if (order.PaymentStatus == "Pending")
                {
                    order.PaymentStatus = "Expired";
                    order.UpdatedAt = now;
                    _context.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = order.Status,
                        Note = $"Phiên thanh toán MoMo ({latestTx.RequestId}) đã hết thời gian hiệu lực 2 phút (Expired).",
                        CreatedAt = now
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        return ApiResponse<PaymentTransactionDto>.Ok(MapToDto(latestTx, order.OrderCode));
    }

    private async Task TryVerifyWithMomoGatewayBeforeExpireAsync(PaymentTransaction tx, Order order)
    {
        try
        {
            var partnerCode = GetPartnerCode();
            var accessKey = GetAccessKey();
            var secretKey = GetSecretKey();
            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)) return;

            var rawSig = $"accessKey={accessKey}&orderId={tx.MomoOrderId}&partnerCode={partnerCode}&requestId={tx.RequestId}";
            var signature = ComputeHmacSha256(rawSig, secretKey);

            var payload = new
            {
                partnerCode,
                requestId = tx.RequestId,
                orderId = tx.MomoOrderId,
                lang = "vi",
                signature
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _httpClient.PostAsync(GetQueryEndpoint(), content);
            if (!res.IsSuccessStatusCode) return;

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("resultCode", out var rcProp) && rcProp.GetInt32() == 0)
            {
                var transId = doc.RootElement.TryGetProperty("transId", out var tProp) ? tProp.ToString() : $"MOMO{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                tx.PaymentStatus = "Success";
                tx.TransactionId = transId;
                tx.PaidAt = DateTime.UtcNow;
                tx.TransactionResponse = json;

                order.PaymentStatus = "Success";
                order.PaidAt = tx.PaidAt;
                if (order.Status == "Pending") order.Status = "Confirmed";
                order.UpdatedAt = DateTime.UtcNow;
            }
        }
        catch
        {
            // Ignore remote query timeout
        }
    }

    public async Task<ApiResponse<PaymentTransactionDto>> ProcessIpnCallbackAsync(MomoIpnCallbackRequest callback, bool skipSignatureCheck = false)
    {
        await _ipnLock.WaitAsync();
        try
        {
            var accessKey = GetAccessKey();
            var secretKey = GetSecretKey();

            // 1. Xác thực chữ ký HMAC SHA256
            if (!skipSignatureCheck)
            {
                var expectedRaw = BuildIpnSignatureRawData(callback, accessKey);
                var expectedSignature = ComputeHmacSha256(expectedRaw, secretKey);

                if (!string.Equals(expectedSignature, callback.Signature, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("[MoMo IPN] Chữ ký không hợp lệ cho OrderId={OrderId}, RequestId={RequestId}.", callback.OrderId, callback.RequestId);
                    return ApiResponse<PaymentTransactionDto>.Fail("Chữ ký xác thực MoMo (HMAC SHA256) không hợp lệ.");
                }
            }

            // 2. Tìm giao dịch theo RequestId hoặc MomoOrderId
            var tx = await _context.PaymentTransactions
                .Include(pt => pt.Order)
                    .ThenInclude(o => o!.StatusHistories)
                .FirstOrDefaultAsync(pt => pt.RequestId == callback.RequestId || pt.MomoOrderId == callback.OrderId);

            if (tx == null || tx.Order == null)
            {
                return ApiResponse<PaymentTransactionDto>.Fail("Không tìm thấy giao dịch hoặc đơn hàng tương ứng.");
            }

            var order = tx.Order;

            // 3. Chống xử lý callback trùng lặp & Không bao giờ ghi đè trạng thái Success bằng Expired hoặc Failed
            if (tx.PaymentStatus == "Success" || order.PaymentStatus is "Success" or "Paid")
            {
                return ApiResponse<PaymentTransactionDto>.Ok(
                    MapToDto(tx, order.OrderCode),
                    "Giao dịch đã được xử lý thành công trước đó (Idempotent Callback).");
            }

            // 4. Kiểm tra khớp số tiền giao dịch
            long expectedAmount = (long)Math.Round(tx.Amount);
            if (callback.Amount != expectedAmount)
            {
                _logger.LogWarning("[MoMo IPN] Sai lệch số tiền cho đơn {OrderCode}: Mong đợi {Expected}, nhận {Actual}", order.OrderCode, expectedAmount, callback.Amount);
                return ApiResponse<PaymentTransactionDto>.Fail($"Số tiền giao dịch không khớp ({callback.Amount:N0}đ != {expectedAmount:N0}đ).");
            }

            var now = DateTime.UtcNow;
            var callbackJson = JsonSerializer.Serialize(callback);
            tx.TransactionResponse = callbackJson;
            if (callback.TransId > 0)
            {
                tx.TransactionId = callback.TransId.ToString();
            }

            // 5. Phân tích ResultCode từ MoMo
            if (callback.ResultCode == 0)
            {
                // Thanh toán thành công (Kể cả callback đến muộn sau khi phiên chuyển sang Expired, vẫn đối soát và công nhận nếu tiền đã trừ thành công)
                bool wasLateCallback = tx.PaymentStatus == "Expired" || now > tx.PaymentExpiresAt;

                tx.PaymentStatus = "Success";
                tx.PaidAt = now;
                if (string.IsNullOrWhiteSpace(tx.TransactionId))
                {
                    tx.TransactionId = $"MOMO{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                }

                order.PaymentStatus = "Success";
                order.PaidAt = now;
                if (order.Status == "Pending")
                {
                    order.Status = "Confirmed";
                }
                order.UpdatedAt = now;

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = order.Status,
                    Note = wasLateCallback
                        ? $"[MoMo IPN - Đối soát Callback đến muộn] Xác nhận thanh toán thành công {tx.Amount:N0}đ. Mã GD MoMo: {tx.TransactionId}."
                        : $"[MoMo IPN Real-Time] Thanh toán thành công {tx.Amount:N0}đ qua Ví MoMo. Mã GD: {tx.TransactionId}.",
                    CreatedAt = now
                });

                await _context.SaveChangesAsync();
                return ApiResponse<PaymentTransactionDto>.Ok(MapToDto(tx, order.OrderCode), "Xác nhận thanh toán MoMo thành công!");
            }
            else if (callback.ResultCode == 1006 || callback.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase) || callback.Message.Contains("hủy", StringComparison.OrdinalIgnoreCase))
            {
                // Khách hàng chủ động hủy giao dịch trên cổng MoMo (1006: User rejected/cancelled)
                tx.PaymentStatus = "Cancelled";
                order.PaymentStatus = "Cancelled";
                order.UpdatedAt = now;

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = order.Status,
                    Note = $"[MoMo IPN] Khách hàng đã hủy phiên thanh toán MoMo (Mã lỗi {callback.ResultCode}: {callback.Message}).",
                    CreatedAt = now
                });

                await _context.SaveChangesAsync();
                return ApiResponse<PaymentTransactionDto>.Ok(MapToDto(tx, order.OrderCode), "Khách hàng đã hủy giao dịch thanh toán MoMo.");
            }
            else
            {
                // Giao dịch thất bại từ MoMo
                tx.PaymentStatus = "Failed";
                order.PaymentStatus = "Failed";
                order.UpdatedAt = now;

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = order.Status,
                    Note = $"[MoMo IPN] Thanh toán MoMo thất bại (Mã lỗi {callback.ResultCode}: {callback.Message}).",
                    CreatedAt = now
                });

                await _context.SaveChangesAsync();
                return ApiResponse<PaymentTransactionDto>.Ok(MapToDto(tx, order.OrderCode), $"Giao dịch MoMo thất bại: {callback.Message}");
            }
        }
        finally
        {
            _ipnLock.Release();
        }
    }

    public async Task<ApiResponse<PaymentTransactionDto>> QueryMomoTransactionStatusAsync(string requestId)
    {
        var tx = await _context.PaymentTransactions
            .Include(pt => pt.Order)
            .FirstOrDefaultAsync(pt => pt.RequestId == requestId);

        if (tx == null || tx.Order == null)
        {
            return ApiResponse<PaymentTransactionDto>.Fail("Không tìm thấy phiên giao dịch MoMo.");
        }

        return await GetPaymentStatusAsync(tx.OrderId);
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction tx, string orderCode)
    {
        var now = DateTime.UtcNow;
        int remaining = tx.PaymentStatus == "Pending"
            ? Math.Max(0, (int)Math.Ceiling((tx.PaymentExpiresAt - now).TotalSeconds))
            : 0;

        bool isExpired = tx.PaymentStatus == "Expired" || (tx.PaymentStatus == "Pending" && remaining <= 0);

        return new PaymentTransactionDto
        {
            Id = tx.Id,
            OrderId = tx.OrderId,
            OrderCode = orderCode,
            MomoOrderId = tx.MomoOrderId,
            RequestId = tx.RequestId,
            TransactionId = tx.TransactionId,
            PaymentMethod = tx.PaymentMethod,
            Amount = tx.Amount,
            PaymentStatus = isExpired && tx.PaymentStatus == "Pending" ? "Expired" : tx.PaymentStatus,
            PayUrl = tx.PayUrl,
            QrCodeUrl = tx.QrCodeUrl,
            Deeplink = tx.Deeplink,
            CreatedAt = DateTime.SpecifyKind(tx.CreatedAt, DateTimeKind.Utc),
            PaymentExpiresAt = DateTime.SpecifyKind(tx.PaymentExpiresAt, DateTimeKind.Utc),
            PaidAt = tx.PaidAt.HasValue ? DateTime.SpecifyKind(tx.PaidAt.Value, DateTimeKind.Utc) : null,
            RemainingSeconds = remaining,
            ServerTimeUtc = now,
            IsExpired = isExpired,
            Message = isExpired
                ? "Phiên thanh toán đã hết hạn. Vui lòng tạo yêu cầu thanh toán mới."
                : tx.PaymentStatus == "Success"
                    ? "Thanh toán MoMo thành công."
                    : "Đang chờ khách hàng thanh toán qua MoMo."
        };
    }
}
