using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public class SePayService : ISePayService
{
    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<SePayService> _logger;
    private static readonly SemaphoreSlim _webhookLock = new(1, 1);

    public SePayService(
        MilkyVerseDbContext context,
        IConfiguration config,
        ILogger<SePayService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    private string GetAccountNumber() =>
        Environment.GetEnvironmentVariable("SEPAY_ACCOUNT_NUMBER")
        ?? _config["SePay:AccountNumber"]
        ?? "00000546"; // Số tài khoản TPBank của bạn

    private string GetAccountName() =>
        Environment.GetEnvironmentVariable("SEPAY_ACCOUNT_NAME")
        ?? _config["SePay:AccountName"]
        ?? "HA BAO TRAN";

    private string GetBank() =>
        Environment.GetEnvironmentVariable("SEPAY_BANK")
        ?? _config["SePay:Bank"]
        ?? "TPBank";

    private string GetApiKey() =>
        Environment.GetEnvironmentVariable("SEPAY_API_KEY")
        ?? _config["SePay:ApiKey"]
        ?? string.Empty;

    public async Task<ApiResponse<SePayQrResponse>> GetOrCreateSePayQrAsync(int orderId, int? userId = null)
    {
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return ApiResponse<SePayQrResponse>.Fail("Không tìm thấy đơn hàng.");
        }

        if (order.Status == "Cancelled")
        {
            return ApiResponse<SePayQrResponse>.Fail("Đơn hàng đã bị hủy.");
        }

        var bank = GetBank();
        var accountNumber = GetAccountNumber();
        var accountName = GetAccountName();
        var amount = order.TotalAmount;
        var description = $"MILKY {order.OrderCode}";

        // Chuẩn URL VietQR của SePay tự động điền số tài khoản, số tiền và nội dung chuyển khoản
        var qrUrl = $"https://qr.sepay.vn/img?acc={Uri.EscapeDataString(accountNumber)}&bank={Uri.EscapeDataString(bank)}&amount={(long)Math.Round(amount)}&des={Uri.EscapeDataString(description)}&template=compact";

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(15);

        // Lưu / cập nhật giao dịch SePay
        var tx = order.PaymentTransactions
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.PaymentMethod == "SEPAY" && p.PaymentStatus == "Pending");

        if (tx == null)
        {
            tx = new PaymentTransaction
            {
                OrderId = order.Id,
                UserId = userId ?? order.UserId,
                MomoOrderId = string.Empty,
                RequestId = $"SEPAY_{order.OrderCode}",
                TransactionId = string.Empty,
                PaymentMethod = "SEPAY",
                Amount = amount,
                PaymentStatus = "Pending",
                PayUrl = qrUrl,
                QrCodeUrl = qrUrl,
                CreatedAt = now,
                PaymentExpiresAt = expiresAt
            };
            _context.PaymentTransactions.Add(tx);
        }

        order.PaymentMethod = "SEPAY";
        order.PaymentExpiresAt = expiresAt;
        order.UpdatedAt = now;

        await _context.SaveChangesAsync();

        int remaining = Math.Max(0, (int)Math.Ceiling((expiresAt - now).TotalSeconds));

        return ApiResponse<SePayQrResponse>.Ok(new SePayQrResponse
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            Amount = amount,
            Description = description,
            Bank = bank,
            AccountNumber = accountNumber,
            AccountName = accountName,
            QrUrl = qrUrl,
            PaymentStatus = order.PaymentStatus,
            ExpiredSeconds = remaining
        }, "Tạo mã thanh toán SePay TPBank thành công.");
    }

    public async Task<ApiResponse<SePayQrResponse>> GetSePayStatusAsync(int orderId, int? userId = null)
    {
        var order = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return ApiResponse<SePayQrResponse>.Fail("Không tìm thấy đơn hàng.");
        }

        var bank = GetBank();
        var accountNumber = GetAccountNumber();
        var accountName = GetAccountName();
        var description = $"MILKY {order.OrderCode}";
        var qrUrl = $"https://qr.sepay.vn/img?acc={Uri.EscapeDataString(accountNumber)}&bank={Uri.EscapeDataString(bank)}&amount={(long)Math.Round(order.TotalAmount)}&des={Uri.EscapeDataString(description)}&template=compact";

        var tx = order.PaymentTransactions.OrderByDescending(p => p.CreatedAt).FirstOrDefault(p => p.PaymentMethod == "SEPAY");

        return ApiResponse<SePayQrResponse>.Ok(new SePayQrResponse
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            Amount = order.TotalAmount,
            Description = description,
            Bank = bank,
            AccountNumber = accountNumber,
            AccountName = accountName,
            QrUrl = qrUrl,
            PaymentStatus = order.PaymentStatus,
            ExpiredSeconds = tx != null ? Math.Max(0, (int)Math.Ceiling((tx.PaymentExpiresAt - DateTime.UtcNow).TotalSeconds)) : 900
        });
    }

    public async Task<ApiResponse<bool>> ProcessSePayWebhookAsync(SePayWebhookRequest request, string? authHeader)
    {
        await _webhookLock.WaitAsync();
        try
        {
            // 1. Kiểm tra API Key nếu đã cấu hình
            var expectedApiKey = GetApiKey();
            if (!string.IsNullOrWhiteSpace(expectedApiKey))
            {
                var cleanToken = (authHeader ?? "").Replace("Apikey ", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (!string.Equals(cleanToken, expectedApiKey, StringComparison.Ordinal))
                {
                    _logger.LogWarning("[SePay Webhook] API Key không hợp lệ: {Token}", cleanToken);
                    return ApiResponse<bool>.Fail("Unauthorized SePay webhook API key.");
                }
            }

            // 2. Chỉ xử lý tiền vào (transferType == "in")
            if (!string.IsNullOrWhiteSpace(request.TransferType) && !request.TransferType.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<bool>.Ok(true, "Bỏ qua giao dịch tiền ra.");
            }

            // 3. Tìm mã đơn hàng từ nội dung chuyển khoản (Content hoặc Description)
            var text = $"{request.Content} {request.Description}".ToUpperInvariant();
            _logger.LogInformation("[SePay Webhook] Nhận biến động số dư: {Amount:N0}đ, Nội dung: {Content}", request.TransferAmount, text);

            Order? order = null;

            // Tìm khớp mã đơn dạng MV2026...
            var match = Regex.Match(text, @"MV[0-9A-Z]{10,20}");
            if (match.Success)
            {
                string code = match.Value;
                order = await _context.Orders
                    .Include(o => o.PaymentTransactions)
                    .Include(o => o.StatusHistories)
                    .FirstOrDefaultAsync(o => o.OrderCode == code);
            }

            // Nếu không tìm thấy, thử tìm khớp ID đơn hàng
            if (order == null)
            {
                var idMatch = Regex.Match(text, @"MILKY\s*([0-9]+)");
                if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var orderId))
                {
                    order = await _context.Orders
                        .Include(o => o.PaymentTransactions)
                        .Include(o => o.StatusHistories)
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                }
            }

            if (order == null)
            {
                _logger.LogWarning("[SePay Webhook] Không tìm thấy đơn hàng trong nội dung giao dịch: {Text}", text);
                return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng khớp với nội dung chuyển khoản.");
            }

            // 4. Tính Idempotency (Chống xử lý lặp lại)
            if (order.PaymentStatus is "Paid" or "Success")
            {
                _logger.LogInformation("[SePay Webhook] Đơn #{OrderCode} đã thanh toán trước đó (Idempotent).", order.OrderCode);
                return ApiResponse<bool>.Ok(true, "Đơn hàng đã được thanh toán trước đó.");
            }

            // 5. Kiểm tra số tiền chuyển khoản có đủ với tổng đơn không
            decimal orderTotal = order.TotalAmount;
            if (request.TransferAmount < orderTotal)
            {
                _logger.LogWarning("[SePay Webhook] Số tiền {Received}đ nhỏ hơn tổng đơn {Total}đ", request.TransferAmount, orderTotal);
                return ApiResponse<bool>.Fail("Số tiền nhận được chưa đủ với tổng đơn hàng.");
            }

            // 6. Cập nhật trạng thái thành công
            var now = DateTime.UtcNow;
            order.PaymentStatus = "Paid";
            order.PaidAt = now;
            order.UpdatedAt = now;
            if (order.Status == "Pending")
            {
                order.Status = "Confirmed";
            }

            var tx = order.PaymentTransactions
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault(p => p.PaymentMethod == "SEPAY" || p.PaymentMethod == "MOMO");

            if (tx != null)
            {
                tx.PaymentStatus = "Success";
                tx.PaidAt = now;
                tx.TransactionId = request.ReferenceCode ?? request.Id.ToString();
            }

            order.StatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                Note = $"[SePay TPBank] Nhận tiền thật {request.TransferAmount:N0}đ thành công qua TPBank. Mã GD: {request.ReferenceCode ?? request.Id.ToString()}.",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            _logger.LogInformation("[SePay Webhook] XÁC NHẬN THÀNH CÔNG ĐƠN HÀNG #{OrderCode}!", order.OrderCode);

            return ApiResponse<bool>.Ok(true, "Xác nhận thanh toán thành công.");
        }
        finally
        {
            _webhookLock.Release();
        }
    }
}
