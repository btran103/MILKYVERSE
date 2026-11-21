using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrEmpty(role) && role != "Customer" && role != "Staff")
        {
            return BadRequest(ApiResponse<OrderResponse>.Fail($"Tài khoản vai trò '{role}' không có quyền đặt món. Chỉ tài khoản Khách hàng và Nhân viên mới có quyền đặt món."));
        }

        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id))
        {
            userId = id;
        }

        var result = await _orderService.CreateOrderAsync(userId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        // Staff, Manager, Admin, Warehouse, Shipper can view store orders; Customers only view their own
        bool isStaffOrAdmin = role is "Admin" or "Manager" or "Staff" or "WarehouseStaff" or "DeliveryStaff";

        if (!isStaffOrAdmin)
        {
            if (int.TryParse(userIdStr, out var id))
            {
                userId = id;
            }
            else
            {
                return Unauthorized(ApiResponse<List<OrderResponse>>.Fail("Bạn cần đăng nhập để xem lịch sử đơn hàng."));
            }
        }

        var result = await _orderService.GetOrdersAsync(userId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        bool isStaffOrAdmin = role is "Admin" or "Manager" or "Staff" or "WarehouseStaff" or "DeliveryStaff";

        if (!isStaffOrAdmin)
        {
            if (int.TryParse(userIdStr, out var uId))
            {
                userId = uId;
            }
        }

        var result = await _orderService.GetOrderByIdAsync(id, userId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Manager,Staff,DeliveryStaff")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var uId))
        {
            userId = uId;
        }

        var result = await _orderService.CancelOrderAsync(id, userId, request?.Reason ?? "Khách hàng tự hủy đơn trong vòng 2 phút");
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("validate-promo")]
    public async Task<IActionResult> ValidatePromotion([FromBody] ValidatePromoRequest request)
    {
        var result = await _orderService.ValidatePromotionAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Webhook nhận thông báo xác thực thanh toán tự động qua Ngrok (SePay, Casso, VietQR, MoMo, hoặc giả lập)
    /// POST: https://&lt;your-ngrok-domain&gt;.ngrok-free.app/api/orders/payment-webhook
    /// </summary>
    [HttpPost("payment-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPaymentWebhook([FromBody] PaymentWebhookRequest request)
    {
        var ordersRes = await _orderService.GetOrdersAsync(null);
        var allOrders = ordersRes.Data ?? new List<OrderResponse>();

        OrderResponse? target = null;
        if (request.OrderId.HasValue && request.OrderId.Value > 0)
        {
            target = allOrders.FirstOrDefault(o => o.Id == request.OrderId.Value);
        }
        if (target == null && !string.IsNullOrWhiteSpace(request.OrderCode))
        {
            var cleanCode = request.OrderCode.Trim().Replace("MILKY", "", StringComparison.OrdinalIgnoreCase).Replace("#", "");
            target = allOrders.FirstOrDefault(o =>
                o.OrderCode.Equals(cleanCode, StringComparison.OrdinalIgnoreCase) ||
                cleanCode.Contains(o.OrderCode, StringComparison.OrdinalIgnoreCase));
        }

        if (target == null)
        {
            return NotFound(ApiResponse<OrderResponse>.Fail("Không tìm thấy đơn hàng tương ứng với mã giao dịch Webhook."));
        }

        if (target.Status == "Cancelled")
        {
            return BadRequest(ApiResponse<OrderResponse>.Fail("Đơn hàng đã bị hủy hoặc hết thời gian thanh toán."));
        }

        var provider = string.IsNullOrWhiteSpace(request.Provider) ? "Ngrok Webhook / VietQR Napas247" : request.Provider;
        var txRef = string.IsNullOrWhiteSpace(request.TransactionRef) ? $"TX{DateTime.UtcNow:HHmmss}" : request.TransactionRef;

        var updateRes = await _orderService.UpdateOrderStatusAsync(target.Id, new UpdateOrderStatusRequest
        {
            Status = target.Status == "Pending" ? "Confirmed" : target.Status,
            PaymentStatus = "Paid",
            Note = $"[Xác thực tự động qua {provider}] Mã GD: {txRef} - Đã nhận đủ {target.TotalAmount:N0}đ."
        });

        return Ok(updateRes);
    }

    /// <summary>
    /// Trang xác nhận nhanh trên điện thoại khi quét mã QR qua Ngrok Public URL
    /// GET: https://&lt;your-ngrok-domain&gt;.ngrok-free.app/api/orders/ngrok-confirm/{orderCode}
    /// </summary>
    [HttpGet("ngrok-confirm/{orderCode}")]
    [AllowAnonymous]
    public async Task<ContentResult> NgrokMobileConfirmPage(string orderCode)
    {
        var ordersRes = await _orderService.GetOrdersAsync(null);
        var target = (ordersRes.Data ?? new List<OrderResponse>())
            .FirstOrDefault(o => o.OrderCode.Equals(orderCode, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            return Content("<html><body style='font-family:sans-serif;text-align:center;padding:40px;'><h2>Không tìm thấy đơn hàng</h2></body></html>", "text/html; charset=utf-8");
        }

        await _orderService.UpdateOrderStatusAsync(target.Id, new UpdateOrderStatusRequest
        {
            Status = target.Status == "Pending" ? "Confirmed" : target.Status,
            PaymentStatus = "Paid",
            Note = $"[Ngrok Mobile QR] Xác thực thanh toán trực tiếp từ thiết bị di động cho đơn #{target.OrderCode}."
        });

        var html = $@"<!DOCTYPE html>
<html lang='vi'>
<head>
  <meta charset='utf-8'/>
  <meta name='viewport' content='width=device-width, initial-scale=1'/>
  <title>Xác Thực Thanh Toán MilkyVerse</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #FAF7F2; color: #2D2522; display: flex; align-items: center; justify-content: center; min-height: 90vh; margin: 0; padding: 16px; }}
    .card {{ background: #fff; border: 2px solid #4E7D55; border-radius: 24px; padding: 32px 24px; max-width: 400px; width: 100%; text-align: center; box-shadow: 0 12px 30px rgba(0,0,0,0.08); }}
    .badge {{ display: inline-block; background: #E6F4EA; color: #1E7E34; font-weight: 800; padding: 8px 16px; border-radius: 999px; font-size: 13px; margin-bottom: 16px; }}
    h1 {{ font-size: 22px; margin: 0 0 8px; color: #2D2522; }}
    .price {{ font-size: 28px; font-weight: 900; color: #6F4E37; margin: 16px 0; }}
    p {{ font-size: 13px; color: #786C65; line-height: 1.5; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='badge'>✓ NGÂN HÀ MILKYVERSE • NGROK WEBHOOK</div>
    <h1>Thanh Toán Thành Công!</h1>
    <p>Mã đơn hàng: <strong>#{target.OrderCode}</strong></p>
    <div class='price'>{target.TotalAmount:N0}đ</div>
    <p>Hệ thống đã gửi tín hiệu xác thực Realtime về màn hình máy tính của bạn. Đơn hàng đã chuyển sang trạng thái <strong>Đã Thanh Toán &amp; Đang Pha Chế</strong>!</p>
  </div>
</body>
</html>";
        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Tự động phát hiện Ngrok Tunnel đang chạy trên máy (cổng 4040 của Ngrok)
    /// </summary>
    [HttpGet("ngrok-tunnel")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveNgrokTunnel()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1.5) };
            var json = await http.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tunnels", out var tunnels) && tunnels.GetArrayLength() > 0)
            {
                foreach (var t in tunnels.EnumerateArray())
                {
                    if (t.TryGetProperty("public_url", out var pubUrl))
                    {
                        var url = pubUrl.GetString();
                        if (!string.IsNullOrEmpty(url) && url.StartsWith("https://"))
                        {
                            return Ok(new { isRunning = true, publicUrl = url, webhookUrl = $"{url}/api/orders/payment-webhook" });
                        }
                    }
                }
            }
        }
        catch
        {
            // Ngrok local dashboard (4040) chưa bật
        }

        return Ok(new { isRunning = false, publicUrl = (string?)null, webhookUrl = "http://localhost:5000/api/orders/payment-webhook" });
    }
}

public class PaymentWebhookRequest
{
    public int? OrderId { get; set; }
    public string? OrderCode { get; set; }
    public decimal? Amount { get; set; }
    public string? TransactionRef { get; set; }
    public string? Provider { get; set; }
}
