using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IAdminService
{
    Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync();
    Task<ApiResponse<List<PromotionDto>>> GetPromotionsAsync();
    Task<ApiResponse<PromotionDto>> CreatePromotionAsync(PromotionDto dto);
    Task<ApiResponse<List<UserProfileDto>>> GetCustomersAsync();
    Task<ApiResponse<List<UserProfileDto>>> GetUsersAsync(string? role = null);
    Task<ApiResponse<UserProfileDto>> CreateEmployeeAsync(CreateUserRequest request);
    Task<ApiResponse<UserProfileDto>> UpdateUserRoleAsync(int userId, UpdateUserRoleRequest request);
}

public class AdminService : IAdminService
{
    private readonly MilkyVerseDbContext _context;

    public AdminService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync()
    {
        var revenue = await _context.Orders
            .Where(o => o.Status == "Completed" || o.PaymentStatus == "Paid")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var totalOrders = await _context.Orders.CountAsync();

        var processingOrders = await _context.Orders
            .CountAsync(o => o.Status == "Pending" || o.Status == "Confirmed" || o.Status == "Preparing" || o.Status == "Delivering");

        var pendingOrdersCount = await _context.Orders
            .CountAsync(o => o.Status == "Pending");

        var deliveringOrdersCount = await _context.Orders
            .CountAsync(o => o.Status == "Delivering");

        var totalCustomers = await _context.Users
            .CountAsync(u => u.Role == "Customer");

        var lowStock = await _context.Ingredients
            .Where(i => i.IsActive && i.CurrentStock <= i.MinAlertThreshold)
            .Select(i => new IngredientDto
            {
                Id = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                CurrentStock = i.CurrentStock,
                MinAlertThreshold = i.MinAlertThreshold,
                UnitCost = i.UnitCost,
                CategoryType = i.CategoryType
            })
            .ToListAsync();

        var recentOrders = await _context.Orders
            .Include(o => o.OrderDetails)
            .Include(o => o.DeliveryAssignment)
                .ThenInclude(da => da!.Shipper)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new OrderResponse
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                ShippingAddress = o.ShippingAddress,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                CreatedAt = o.CreatedAt,
                ShipperName = o.DeliveryAssignment != null && o.DeliveryAssignment.Shipper != null ? o.DeliveryAssignment.Shipper.FullName : "",
                DeliveryStatus = o.DeliveryAssignment != null ? o.DeliveryAssignment.Status : ""
            })
            .ToListAsync();

        var topProducts = await _context.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsBestSeller)
            .Take(6)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                Name = p.Name,
                Slug = p.Slug,
                BasePrice = p.BasePrice,
                ImageUrl = p.ImageUrl,
                IsBestSeller = p.IsBestSeller,
                IsNew = p.IsNew,
                IsActive = p.IsActive,
                StockStatus = p.StockStatus,
                ProductType = p.ProductType
            })
            .ToListAsync();

        var summary = new DashboardSummaryDto
        {
            TotalRevenue = revenue,
            TotalOrders = totalOrders,
            ProcessingOrders = processingOrders,
            PendingOrdersCount = pendingOrdersCount,
            DeliveringOrdersCount = deliveringOrdersCount,
            TotalCustomers = totalCustomers,
            LowStockCount = lowStock.Count,
            LowStockIngredients = lowStock,
            RecentOrders = recentOrders,
            TopProducts = topProducts
        };

        return ApiResponse<DashboardSummaryDto>.Ok(summary);
    }

    public async Task<ApiResponse<List<PromotionDto>>> GetPromotionsAsync()
    {
        var promos = await _context.Promotions
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PromotionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                DiscountPercent = p.DiscountPercent,
                DiscountAmount = p.DiscountAmount,
                MinOrderValue = p.MinOrderValue,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return ApiResponse<List<PromotionDto>>.Ok(promos);
    }

    public async Task<ApiResponse<PromotionDto>> CreatePromotionAsync(PromotionDto dto)
    {
        var promo = new Promotion
        {
            Code = dto.Code.Trim().ToUpper(),
            Description = dto.Description,
            DiscountPercent = dto.DiscountPercent,
            DiscountAmount = dto.DiscountAmount,
            MinOrderValue = dto.MinOrderValue,
            MaxUsage = 1000,
            UsedCount = 0,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(3),
            IsActive = true
        };

        _context.Promotions.Add(promo);
        await _context.SaveChangesAsync();

        dto.Id = promo.Id;
        return ApiResponse<PromotionDto>.Ok(dto, "Tạo khuyến mãi thành công!");
    }

    public async Task<ApiResponse<List<UserProfileDto>>> GetCustomersAsync()
    {
        var list = await _context.Users
            .Where(u => u.Role == "Customer")
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<UserProfileDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<UserProfileDto>>> GetUsersAsync(string? role = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role.ToLower() == role.ToLower());
        }

        var list = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<UserProfileDto>>.Ok(list);
    }

    public async Task<ApiResponse<UserProfileDto>> CreateEmployeeAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserProfileDto>.Fail("Email và mật khẩu không được để trống.");
        }

        var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existing)
        {
            return ApiResponse<UserProfileDto>.Fail("Email này đã được sử dụng trong hệ thống.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber ?? "",
            Address = request.Address ?? "",
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Staff" : request.Role,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }, $"Tạo tài khoản nhân viên vai trò {user.Role} thành công!");
    }

    public async Task<ApiResponse<UserProfileDto>> UpdateUserRoleAsync(int userId, UpdateUserRoleRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("Không tìm thấy người dùng.");
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            user.Role = request.Role;
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        }, "Cập nhật phân quyền và trạng thái nhân viên thành công!");
    }
}
