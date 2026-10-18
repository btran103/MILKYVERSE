using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager,Staff,WarehouseStaff")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IInventoryService _inventoryService;
    private readonly IDeliveryService _deliveryService;

    public AdminController(
        IAdminService adminService,
        IInventoryService inventoryService,
        IDeliveryService deliveryService)
    {
        _adminService = adminService;
        _inventoryService = inventoryService;
        _deliveryService = deliveryService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _adminService.GetDashboardSummaryAsync();
        return Ok(result);
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role)
    {
        var result = await _adminService.GetUsersAsync(role);
        return Ok(result);
    }

    [HttpPost("users/employee")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateUserRequest request)
    {
        var result = await _adminService.CreateEmployeeAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("users/{userId:int}/role")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request)
    {
        var result = await _adminService.UpdateUserRoleAsync(userId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // --- Inventory & Ingredients ---
    [HttpGet("ingredients")]
    public async Task<IActionResult> GetIngredients()
    {
        var result = await _inventoryService.GetIngredientsAsync();
        return Ok(result);
    }

    [HttpPost("ingredients")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> CreateIngredient([FromBody] IngredientDto dto)
    {
        var result = await _inventoryService.CreateIngredientAsync(dto);
        return Ok(result);
    }

    [HttpPut("ingredients/{id:int}")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> UpdateIngredient(int id, [FromBody] IngredientDto dto)
    {
        var result = await _inventoryService.UpdateIngredientAsync(id, dto);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("inventory/adjust")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustRequest request)
    {
        var result = await _inventoryService.AdjustStockAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("inventory/transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int? ingredientId)
    {
        var result = await _inventoryService.GetTransactionsAsync(ingredientId);
        return Ok(result);
    }

    // --- FEFO Batches ---
    [HttpGet("inventory/batches")]
    public async Task<IActionResult> GetStockBatches([FromQuery] int? ingredientId)
    {
        var result = await _inventoryService.GetStockBatchesAsync(ingredientId);
        return Ok(result);
    }

    // --- Purchase Receipts (Nhập kho) ---
    [HttpGet("inventory/receipts")]
    public async Task<IActionResult> GetPurchaseReceipts()
    {
        var result = await _inventoryService.GetPurchaseReceiptsAsync();
        return Ok(result);
    }

    [HttpPost("inventory/receipts")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> CreatePurchaseReceipt([FromBody] CreatePurchaseReceiptRequest request)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id)) userId = id;

        var result = await _inventoryService.CreatePurchaseReceiptAsync(userId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // --- Stock Audits (Kiểm kê) ---
    [HttpGet("inventory/audits")]
    public async Task<IActionResult> GetStockAudits()
    {
        var result = await _inventoryService.GetStockAuditsAsync();
        return Ok(result);
    }

    [HttpPost("inventory/audits")]
    [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
    public async Task<IActionResult> CreateStockAudit([FromBody] CreateStockAuditRequest request)
    {
        int? userId = null;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var id)) userId = id;

        var result = await _inventoryService.CreateStockAuditAsync(userId, request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // --- Promotions ---
    [HttpGet("promotions")]
    public async Task<IActionResult> GetPromotions()
    {
        var result = await _adminService.GetPromotionsAsync();
        return Ok(result);
    }

    [HttpPost("promotions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePromotion([FromBody] PromotionDto dto)
    {
        var result = await _adminService.CreatePromotionAsync(dto);
        return Ok(result);
    }

    [HttpGet("customers")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetCustomers()
    {
        var result = await _adminService.GetCustomersAsync();
        return Ok(result);
    }

    [HttpGet("shippers")]
    public async Task<IActionResult> GetShippers()
    {
        var result = await _deliveryService.GetAvailableShippersAsync();
        return Ok(result);
    }
}
