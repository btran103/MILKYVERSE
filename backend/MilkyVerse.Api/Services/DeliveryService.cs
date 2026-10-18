using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IDeliveryService
{
    Task<ApiResponse<ShipperDashboardDto>> GetShipperDashboardAsync(int shipperId);
    Task<ApiResponse<List<OrderResponse>>> GetAssignedOrdersAsync(int shipperId);
    Task<ApiResponse<bool>> AssignShipperAsync(AssignShipperRequest request);
    Task<ApiResponse<bool>> UpdateDeliveryStatusAsync(int shipperId, int orderId, UpdateDeliveryStatusRequest request);
    Task<ApiResponse<bool>> ReconcileCodAsync(ReconcileCodRequest request);
    Task<ApiResponse<List<UserProfileDto>>> GetAvailableShippersAsync();
}

public class DeliveryService : IDeliveryService
{
    private readonly MilkyVerseDbContext _context;

    public DeliveryService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<ShipperDashboardDto>> GetShipperDashboardAsync(int shipperId)
    {
        var assignments = await _context.DeliveryAssignments
            .Include(da => da.Order)
                .ThenInclude(o => o!.OrderDetails)
            .Include(da => da.Shipper)
            .Where(da => da.ShipperId == shipperId)
            .OrderByDescending(da => da.AssignedAt)
            .ToListAsync();

        int totalAssigned = assignments.Count;
        int inTransit = assignments.Count(da => da.Status == "Assigned" || da.Status == "PickedUp" || da.Status == "InTransit");
        int deliveredToday = assignments.Count(da => da.Status == "Delivered" && da.DeliveredAt?.Date == DateTime.UtcNow.Date);
        decimal codCollected = assignments.Where(da => da.IsCodCollected).Sum(da => da.CodAmount);
        decimal codPendingReconcile = assignments.Where(da => da.IsCodCollected && !da.CodReconciled).Sum(da => da.CodAmount);

        var orders = assignments.Select(da => new OrderResponse
        {
            Id = da.Order!.Id,
            OrderCode = da.Order.OrderCode,
            CustomerName = da.Order.CustomerName,
            CustomerPhone = da.Order.CustomerPhone,
            ShippingAddress = da.Order.ShippingAddress,
            TotalAmount = da.Order.TotalAmount,
            Status = da.Order.Status,
            PaymentStatus = da.Order.PaymentStatus,
            PaymentMethod = da.Order.PaymentMethod,
            CreatedAt = da.Order.CreatedAt,
            ShipperId = da.ShipperId,
            ShipperName = da.Shipper?.FullName ?? "",
            ShipperPhone = da.Shipper?.PhoneNumber ?? "",
            DeliveryStatus = da.Status,
            CodAmount = da.CodAmount,
            IsCodCollected = da.IsCodCollected,
            CodReconciled = da.CodReconciled,
            Note = da.Notes
        }).ToList();

        var dto = new ShipperDashboardDto
        {
            TotalAssignedOrders = totalAssigned,
            InTransitOrders = inTransit,
            DeliveredToday = deliveredToday,
            TotalCodCollected = codCollected,
            TotalCodPendingReconcile = codPendingReconcile,
            Orders = orders
        };

        return ApiResponse<ShipperDashboardDto>.Ok(dto);
    }

    public async Task<ApiResponse<List<OrderResponse>>> GetAssignedOrdersAsync(int shipperId)
    {
        var assignments = await _context.DeliveryAssignments
            .Include(da => da.Order)
                .ThenInclude(o => o!.OrderDetails)
            .Include(da => da.Shipper)
            .Where(da => da.ShipperId == shipperId)
            .OrderByDescending(da => da.AssignedAt)
            .ToListAsync();

        var orders = assignments.Select(da => new OrderResponse
        {
            Id = da.Order!.Id,
            OrderCode = da.Order.OrderCode,
            CustomerName = da.Order.CustomerName,
            CustomerPhone = da.Order.CustomerPhone,
            ShippingAddress = da.Order.ShippingAddress,
            TotalAmount = da.Order.TotalAmount,
            Status = da.Order.Status,
            PaymentStatus = da.Order.PaymentStatus,
            PaymentMethod = da.Order.PaymentMethod,
            CreatedAt = da.Order.CreatedAt,
            ShipperId = da.ShipperId,
            ShipperName = da.Shipper?.FullName ?? "",
            ShipperPhone = da.Shipper?.PhoneNumber ?? "",
            DeliveryStatus = da.Status,
            CodAmount = da.CodAmount,
            IsCodCollected = da.IsCodCollected,
            CodReconciled = da.CodReconciled,
            Note = da.Notes
        }).ToList();

        return ApiResponse<List<OrderResponse>>.Ok(orders);
    }

    public async Task<ApiResponse<bool>> AssignShipperAsync(AssignShipperRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.DeliveryAssignment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng.");
        }

        var shipper = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ShipperId && u.Role == "DeliveryStaff");
        if (shipper == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy tài khoản nhân viên giao hàng (Shipper).");
        }

        if (order.DeliveryAssignment == null)
        {
            order.DeliveryAssignment = new DeliveryAssignment
            {
                OrderId = order.Id,
                ShipperId = shipper.Id,
                AssignedAt = DateTime.UtcNow,
                Status = "Assigned",
                CodAmount = order.PaymentMethod == "COD" ? order.TotalAmount : 0,
                Notes = request.Notes
            };
            _context.DeliveryAssignments.Add(order.DeliveryAssignment);
        }
        else
        {
            order.DeliveryAssignment.ShipperId = shipper.Id;
            order.DeliveryAssignment.AssignedAt = DateTime.UtcNow;
            order.DeliveryAssignment.Status = "Assigned";
            order.DeliveryAssignment.Notes = request.Notes;
        }

        order.Status = "Delivering";
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = "Delivering",
            Note = $"Đã bàn giao đơn cho shipper {shipper.FullName} ({shipper.PhoneNumber})",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, $"Phân công đơn {order.OrderCode} cho {shipper.FullName} thành công!");
    }

    public async Task<ApiResponse<bool>> UpdateDeliveryStatusAsync(int shipperId, int orderId, UpdateDeliveryStatusRequest request)
    {
        var assignment = await _context.DeliveryAssignments
            .Include(da => da.Order)
            .FirstOrDefaultAsync(da => da.OrderId == orderId && da.ShipperId == shipperId);

        if (assignment == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng giao được phân công cho bạn.");
        }

        assignment.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Notes)) assignment.Notes = request.Notes;
        if (!string.IsNullOrWhiteSpace(request.ProofImageUrl)) assignment.ProofImageUrl = request.ProofImageUrl;

        if (request.Status == "PickedUp")
        {
            assignment.PickedUpAt = DateTime.UtcNow;
            assignment.Order!.Status = "Delivering";
        }
        else if (request.Status == "Delivered")
        {
            assignment.DeliveredAt = DateTime.UtcNow;
            assignment.Order!.Status = "Completed";
            assignment.IsCodCollected = request.IsCodCollected || (assignment.Order.PaymentMethod == "COD");
            assignment.Order.PaymentStatus = "Paid";
        }
        else if (request.Status == "Failed")
        {
            assignment.FailureReason = request.FailureReason;
            assignment.Order!.Status = "Cancelled";
        }

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = assignment.OrderId,
            Status = assignment.Order!.Status,
            Note = $"Shipper cập nhật: {request.Status}. {request.Notes}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Cập nhật tiến độ giao hàng thành công!");
    }

    public async Task<ApiResponse<bool>> ReconcileCodAsync(ReconcileCodRequest request)
    {
        if (request.OrderIds == null || !request.OrderIds.Any())
        {
            return ApiResponse<bool>.Fail("Vui lòng chọn danh sách đơn cần đối soát COD.");
        }

        var assignments = await _context.DeliveryAssignments
            .Where(da => request.OrderIds.Contains(da.OrderId))
            .ToListAsync();

        foreach (var da in assignments)
        {
            da.CodReconciled = true;
        }

        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, $"Đã hoàn tất đối soát nộp tiền cho {assignments.Count} đơn COD!");
    }

    public async Task<ApiResponse<List<UserProfileDto>>> GetAvailableShippersAsync()
    {
        var shippers = await _context.Users
            .Where(u => u.Role == "DeliveryStaff" && u.IsActive)
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

        return ApiResponse<List<UserProfileDto>>.Ok(shippers);
    }
}
