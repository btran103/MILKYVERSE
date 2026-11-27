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
        int? actorId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;
        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);

        var result = await _orderService.UpdateOrderStatusAsync(id, request, actorId, role, name);
        if (!result.Success)
        {
            if (result.Message.StartsWith("FORBIDDEN:", StringComparison.Ordinal))
                return StatusCode(403, result);
            return BadRequest(result);
        }
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
        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);

        var result = await _orderService.CancelOrderAsync(id, userId, request?.Reason ?? "Khách hàng hủy đơn theo yêu cầu", role, name);
        if (!result.Success)
        {
            if (result.Message.StartsWith("FORBIDDEN:", StringComparison.Ordinal))
                return StatusCode(403, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("validate-promo")]
    public async Task<IActionResult> ValidatePromotion([FromBody] ValidatePromoRequest request)
    {
        var result = await _orderService.ValidatePromotionAsync(request);
        return Ok(result);
    }
}
