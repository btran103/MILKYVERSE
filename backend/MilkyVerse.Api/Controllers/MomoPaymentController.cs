using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/payments/momo")]
public class MomoPaymentController : ControllerBase
{
    private readonly IMomoPaymentService _momoService;
    private readonly MilkyVerseDbContext _context;
    private readonly IConfiguration _config;

    public MomoPaymentController(
        IMomoPaymentService momoService,
        MilkyVerseDbContext context,
        IConfiguration config)
    {
        _momoService = momoService;
        _context = context;
        _config = config;
    }

    /// <summary>
    /// 1. Tạo phiên thanh toán MoMo mới (Hiệu lực chính xác 120 giây = 2 phút)
    /// POST /api/payments/momo/create
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreatePayment([FromBody] CreateMomoPaymentRequest request)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id))
        {
            userId = id;
        }

        var result = await _momoService.CreateMomoPaymentAsync(userId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// 2. Kiểm tra trạng thái phiên thanh toán & thời gian đếm ngược 2 phút theo giờ chuẩn Backend
    /// GET /api/payments/momo/status/{orderId}
    /// </summary>
    [HttpGet("status/{orderId:int}")]
    public async Task<IActionResult> GetStatus(int orderId)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id))
        {
            userId = id;
        }

        var result = await _momoService.GetPaymentStatusAsync(orderId, userId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// 3. IPN Webhook chính thức để MoMo gọi kết quả giao dịch về Backend (Xác thực chữ ký HMAC SHA256)
    /// POST /api/payments/momo/ipn
    /// </summary>
    [HttpPost("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveIpnCallback([FromBody] MomoIpnCallbackRequest callback)
    {
        var result = await _momoService.ProcessIpnCallbackAsync(callback, skipSignatureCheck: false);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// 4. Xác thực kết quả khi khách hàng được Redirect từ cổng MoMo về lại Website
    /// GET /api/payments/momo/return
    /// </summary>
    [HttpGet("return")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleReturnRedirect([FromQuery] MomoIpnCallbackRequest callback)
    {
        var result = await _momoService.ProcessIpnCallbackAsync(callback, skipSignatureCheck: false);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// 5. Cổng Thanh Toán MoMo Sandbox Trực Quan (Cho phép khách hàng quét mã / xác nhận thanh toán / hủy / test các kịch bản MoMo Sandbox với chữ ký HMAC SHA256 thật)
    /// GET /api/payments/momo/sandbox-gateway?requestId=...
    /// </summary>
    [HttpGet("sandbox-gateway")]
    [AllowAnonymous]
    public async Task<ContentResult> RenderMomoSandboxGateway([FromQuery] string requestId)
    {
        var tx = await _context.PaymentTransactions
            .Include(pt => pt.Order)
            .FirstOrDefaultAsync(pt => pt.RequestId == requestId);

        if (tx == null || tx.Order == null)
        {
            return Content("<html><body style='font-family:sans-serif;text-align:center;padding:40px;'><h2>Không tìm thấy phiên giao dịch MoMo</h2></body></html>", "text/html; charset=utf-8");
        }

        var now = DateTime.UtcNow;
        var remainingSecs = tx.PaymentStatus == "Pending"
            ? Math.Max(0, (int)Math.Ceiling((tx.PaymentExpiresAt - now).TotalSeconds))
            : 0;

        if (tx.PaymentStatus == "Pending" && remainingSecs <= 0)
        {
            tx.PaymentStatus = "Expired";
            if (tx.Order.PaymentStatus == "Pending") tx.Order.PaymentStatus = "Expired";
            await _context.SaveChangesAsync();
        }

        bool isExpired = tx.PaymentStatus == "Expired" || remainingSecs <= 0;
        bool isDone = tx.PaymentStatus is "Success" or "Failed" or "Cancelled" or "Refunded";
        string returnUrl = $"http://localhost:3000/orders/{tx.OrderId}";

        var html = $@"<!DOCTYPE html>
<html lang='vi'>
<head>
  <meta charset='utf-8'/>
  <meta name='viewport' content='width=device-width, initial-scale=1'/>
  <title>Cổng Thanh Toán MoMo Sandbox — MILKYVERSE AI</title>
  <style>
    * {{ box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: linear-gradient(135deg, #a50064 0%, #d82d8b 100%); color: #2D2522; margin: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 20px; }}
    .container {{ background: #fff; border-radius: 28px; max-width: 480px; width: 100%; padding: 28px; box-shadow: 0 24px 60px rgba(0,0,0,0.28); }}
    .header {{ display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid #f0e6ec; padding-bottom: 16px; margin-bottom: 18px; }}
    .logo {{ display: flex; align-items: center; gap: 10px; font-weight: 900; font-size: 18px; color: #a50064; }}
    .logo-badge {{ background: #a50064; color: #fff; border-radius: 12px; padding: 6px 10px; font-size: 13px; }}
    .timer-pill {{ background: #fff0f6; color: #a50064; border: 1px solid #f7c6df; padding: 6px 12px; border-radius: 999px; font-family: monospace; font-weight: 900; font-size: 15px; }}
    .info-box {{ background: #faf7f9; border: 1px solid #f0e4ec; border-radius: 18px; padding: 16px; margin-bottom: 18px; font-size: 13px; }}
    .row {{ display: flex; justify-content: space-between; margin-bottom: 8px; }}
    .row:last-child {{ margin-bottom: 0; }}
    .amount {{ font-size: 24px; font-weight: 900; color: #a50064; }}
    .btn {{ width: 100%; border: none; border-radius: 16px; padding: 14px 18px; font-weight: 800; font-size: 14px; cursor: pointer; margin-bottom: 10px; transition: all 0.2s; }}
    .btn-pay {{ background: #a50064; color: #fff; box-shadow: 0 8px 20px rgba(165,0,100,0.25); }}
    .btn-pay:hover {{ background: #880052; }}
    .btn-cancel {{ background: #fff; color: #786C65; border: 1px solid #e0d6dc; }}
    .btn-fail {{ background: #fff5f5; color: #c53030; border: 1px solid #feb2b2; font-size: 12px; padding: 10px; }}
    .expired-box {{ background: #fff5f5; border: 2px solid #fc8181; color: #c53030; padding: 18px; border-radius: 18px; text-align: center; font-weight: 700; margin-bottom: 16px; }}
    .success-box {{ background: #f0fff4; border: 2px solid #68d391; color: #276749; padding: 18px; border-radius: 18px; text-align: center; font-weight: 700; margin-bottom: 16px; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <div class='logo'>
        <span class='logo-badge'>MoMo</span>
        <span>Cổng Thanh Toán Sandbox</span>
      </div>
      <div class='timer-pill' id='countdown'>02:00</div>
    </div>

    <div class='info-box'>
      <div class='row'><span>Đơn vị thụ hưởng:</span><strong>MILKYVERSE AI</strong></div>
      <div class='row'><span>Mã đơn hàng:</span><strong>#{tx.Order.OrderCode}</strong></div>
      <div class='row'><span>Mã giao dịch (RequestId):</span><strong style='font-family:monospace;font-size:11px;'>{tx.RequestId}</strong></div>
      <div class='row'><span>Trạng thái hiện tại:</span><strong id='status-label'>{tx.PaymentStatus}</strong></div>
      <div class='row' style='align-items:baseline;padding-top:8px;border-top:1px dashed #e5d5df;'>
        <span>Số tiền thanh toán:</span>
        <span class='amount'>{tx.Amount:N0}đ</span>
      </div>
    </div>

    <div id='expired-alert' class='expired-box' style='display:{(isExpired ? "block" : "none")};'>
      Phiên thanh toán đã hết hạn. Vui lòng tạo yêu cầu thanh toán mới.
    </div>

    <div id='done-alert' class='success-box' style='display:{(isDone ? "block" : "none")};'>
      Giao dịch đã kết thúc với trạng thái: <strong>{tx.PaymentStatus}</strong>
    </div>

    <div id='action-buttons' style='display:{(!isExpired && !isDone ? "block" : "none")};'>
      <button class='btn btn-pay' onclick='submitAction(""pay"")'>✓ Xác Nhận Thanh Toán MoMo ({tx.Amount:N0}đ)</button>
      <button class='btn btn-cancel' onclick='submitAction(""cancel"")'>✕ Hủy Giao Dịch (Khách hàng hủy)</button>
      <button class='btn btn-fail' onclick='submitAction(""fail"")'>⚠ Giả lập lỗi thanh toán thất bại từ MoMo (ResultCode 1005)</button>
    </div>

    <a href='{returnUrl}' style='display:block;text-align:center;color:#a50064;font-weight:700;font-size:13px;text-decoration:none;margin-top:12px;'>
      ← Quay về trang đơn hàng MilkyVerse
    </a>
  </div>

  <script>
    let remaining = {remainingSecs};
    const cdEl = document.getElementById('countdown');
    const expAlert = document.getElementById('expired-alert');
    const actBtns = document.getElementById('action-buttons');

    function formatMMSS(s) {{
      const m = Math.floor(s / 60).toString().padStart(2, '0');
      const sec = (s % 60).toString().padStart(2, '0');
      return m + ':' + sec;
    }}

    cdEl.textContent = formatMMSS(remaining);

    const timer = setInterval(() => {{
      if (remaining > 0) {{
        remaining--;
        cdEl.textContent = formatMMSS(remaining);
        if (remaining === 0) {{
          clearInterval(timer);
          expAlert.style.display = 'block';
          actBtns.style.display = 'none';
          document.getElementById('status-label').textContent = 'Expired';
        }}
      }}
    }}, 1000);

    async function submitAction(action) {{
      actBtns.style.opacity = '0.5';
      actBtns.style.pointerEvents = 'none';
      try {{
        const res = await fetch('/api/payments/momo/sandbox-execute', {{
          method: 'POST',
          headers: {{ 'Content-Type': 'application/json' }},
          body: JSON.stringify({{ requestId: '{tx.RequestId}', action }})
        }});
        const data = await res.json();
        window.location.href = '{returnUrl}';
      }} catch (e) {{
        alert('Lỗi kết nối tới máy chủ');
        actBtns.style.opacity = '1';
        actBtns.style.pointerEvents = 'auto';
      }}
    }}
  </script>
</body>
</html>";

        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// 6. Thực thi giao dịch từ trang MoMo Sandbox Gateway: Tự động ký HMAC SHA256 chuẩn MoMo và kích hoạt luồng xử lý IPN Webhook
    /// POST /api/payments/momo/sandbox-execute
    /// </summary>
    [HttpPost("sandbox-execute")]
    [AllowAnonymous]
    public async Task<IActionResult> ExecuteSandboxAction([FromBody] MomoSandboxActionRequest req)
    {
        var tx = await _context.PaymentTransactions
            .Include(pt => pt.Order)
            .FirstOrDefaultAsync(pt => pt.RequestId == req.RequestId);

        if (tx == null || tx.Order == null)
        {
            return NotFound(ApiResponse<PaymentTransactionDto>.Fail("Không tìm thấy phiên thanh toán MoMo."));
        }

        // Nếu phiên đã hết hạn (và không phải kịch bản test late-callback), chặn thao tác trên phiên đã hết hạn
        if (!req.AllowLateCallback && (tx.PaymentStatus == "Expired" || DateTime.UtcNow >= tx.PaymentExpiresAt))
        {
            tx.PaymentStatus = "Expired";
            if (tx.Order.PaymentStatus == "Pending") tx.Order.PaymentStatus = "Expired";
            await _context.SaveChangesAsync();
            return BadRequest(ApiResponse<PaymentTransactionDto>.Fail("Phiên thanh toán đã hết hạn. Vui lòng tạo yêu cầu thanh toán mới."));
        }

        var partnerCode = Environment.GetEnvironmentVariable("MOMO_PARTNER_CODE") ?? _config["Momo:PartnerCode"] ?? "MOMO";
        var accessKey = Environment.GetEnvironmentVariable("MOMO_ACCESS_KEY") ?? _config["Momo:AccessKey"] ?? string.Empty;
        var secretKey = Environment.GetEnvironmentVariable("MOMO_SECRET_KEY") ?? _config["Momo:SecretKey"] ?? string.Empty;

        int resultCode = req.Action?.ToLowerInvariant() switch
        {
            "pay" or "success" => 0,
            "cancel" or "cancelled" => 1006,
            _ => 1005
        };

        string message = resultCode switch
        {
            0 => "Thành công.",
            1006 => "Giao dịch bị từ chối bởi người dùng (User cancelled).",
            _ => "Giao dịch thất bại do hết hạn hoặc lỗi nguồn tiền."
        };

        long amountLong = req.OverrideAmount ?? (long)Math.Round(tx.Amount);
        long transId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long responseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string extraData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"orderId\":{tx.OrderId}}}"));

        var ipnPayload = new MomoIpnCallbackRequest
        {
            PartnerCode = partnerCode,
            OrderId = tx.MomoOrderId,
            RequestId = tx.RequestId,
            Amount = amountLong,
            OrderInfo = $"Thanh toan don hang MilkyVerse #{tx.Order.OrderCode}",
            OrderType = "momo_wallet",
            TransId = transId,
            ResultCode = resultCode,
            Message = message,
            PayType = "qr",
            ResponseTime = responseTime,
            ExtraData = extraData
        };

        // Tạo chữ ký HMAC SHA256 thật theo đúng chuẩn MoMo
        var rawSig = _momoService.BuildIpnSignatureRawData(ipnPayload, accessKey);
        ipnPayload.Signature = req.InvalidSignature
            ? "0000000000000000000000000000000000000000000000000000000000000000"
            : _momoService.ComputeHmacSha256(rawSig, secretKey);

        var result = await _momoService.ProcessIpnCallbackAsync(ipnPayload, skipSignatureCheck: false);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public class MomoSandboxActionRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string Action { get; set; } = "pay"; // pay, cancel, fail
    public bool InvalidSignature { get; set; } = false;
    public long? OverrideAmount { get; set; }
    public bool AllowLateCallback { get; set; } = false;
}
