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

    [HttpPost("validate-promo")]
    public async Task<IActionResult> ValidatePromotion([FromBody] ValidatePromoRequest request)
    {
        var result = await _orderService.ValidatePromotionAsync(request);
        return Ok(result);
    }
}
