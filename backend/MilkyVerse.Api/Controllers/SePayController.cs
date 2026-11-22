using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/payments/sepay")]
public class SePayController : ControllerBase
{
    private readonly ISePayService _sePayService;
    private readonly ILogger<SePayController> _logger;

    public SePayController(ISePayService sePayService, ILogger<SePayController> logger)
    {
        _sePayService = sePayService;
        _logger = logger;
    }

    /// <summary>
    /// 1. Lấy mã QR VietQR TPBank tự động cho đơn hàng
    /// GET /api/payments/sepay/qr/{orderId}
    /// </summary>
    [HttpGet("qr/{orderId:int}")]
    public async Task<IActionResult> GetQr(int orderId)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id))
        {
            userId = id;
        }

        var result = await _sePayService.GetOrCreateSePayQrAsync(orderId, userId);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// 2. Lấy trạng thái thanh toán SePay của đơn hàng (Polling từ frontend)
    /// GET /api/payments/sepay/status/{orderId}
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

        var result = await _sePayService.GetSePayStatusAsync(orderId, userId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// 3. Webhook endpoint nhận biến động số dư từ SePay (Tự động nổ đơn khi tiền thật vào TPBank)
    /// POST /api/payments/sepay/webhook
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveSePayWebhook([FromBody] SePayWebhookRequest request)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var result = await _sePayService.ProcessSePayWebhookAsync(request, authHeader);

        // SePay yêu cầu trả về status 200 kèm JSON { success: true }
        return Ok(new
        {
            success = result.Success,
            message = result.Message
        });
    }
}
