using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;

    public DeliveryController(IDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "DeliveryStaff,Manager,Admin")]
    public async Task<IActionResult> GetDashboard()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var shipperId))
        {
            return Unauthorized(ApiResponse<ShipperDashboardDto>.Fail("Không xác định được danh tính shipper."));
        }

        var result = await _deliveryService.GetShipperDashboardAsync(shipperId);
        return Ok(result);
    }

    [HttpGet("assigned")]
    [Authorize(Roles = "DeliveryStaff,Manager,Admin")]
    public async Task<IActionResult> GetAssignedOrders()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var shipperId))
        {
            return Unauthorized(ApiResponse<List<OrderResponse>>.Fail("Không xác định được danh tính shipper."));
        }

        var result = await _deliveryService.GetAssignedOrdersAsync(shipperId);
        return Ok(result);
    }

    [HttpPost("assign")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> AssignShipper([FromBody] AssignShipperRequest request)
    {
        var result = await _deliveryService.AssignShipperAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("orders/{orderId:int}/status")]
    [Authorize(Roles = "DeliveryStaff,Manager,Admin")]
    public async Task<IActionResult> UpdateDeliveryStatus(int orderId, [FromBody] UpdateDeliveryStatusRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var shipperId))
        {
            return Unauthorized(ApiResponse<bool>.Fail("Không xác định được danh tính shipper."));
        }

        var result = await _deliveryService.UpdateDeliveryStatusAsync(shipperId, orderId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("reconcile-cod")]
    [Authorize(Roles = "Admin,Manager,DeliveryStaff")]
    public async Task<IActionResult> ReconcileCod([FromBody] ReconcileCodRequest request)
    {
        var result = await _deliveryService.ReconcileCodAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
