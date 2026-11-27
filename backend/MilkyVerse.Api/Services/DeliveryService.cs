using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IDeliveryService
{
    Task<ApiResponse<ShipperDashboardDto>> GetShipperDashboardAsync(int shipperId);
    Task<ApiResponse<List<OrderResponse>>> GetAssignedOrdersAsync(int shipperId);
    Task<ApiResponse<List<OrderResponse>>> GetReadyForPickupPoolAsync();
    Task<ApiResponse<bool>> ClaimOrderForDeliveryAsync(int shipperId, int orderId, string? notes = null);
    Task<ApiResponse<bool>> AssignShipperAsync(AssignShipperRequest request, int? actorUserId = null, string? actorName = null, string? actorRole = null);
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
                    .ThenInclude(od => od.Options)
            .Include(da => da.Order)
                .ThenInclude(o => o!.OrderDetails)
                    .ThenInclude(od => od.Product)
            .Include(da => da.Order)
                .ThenInclude(o => o!.StatusHistories)
            .Include(da => da.Order)
                .ThenInclude(o => o!.PaymentTransactions)
            .Include(da => da.Shipper)
            .Where(da => da.ShipperId == shipperId && da.Order != null)
            .OrderByDescending(da => da.AssignedAt)
            .ToListAsync();

        int totalAssigned = assignments.Count;
        int inTransit = assignments.Count(da => da.Status == "Assigned" || da.Status == "PickedUp" || da.Status == "InTransit");
        int deliveredToday = assignments.Count(da => da.Status == "Delivered" && da.DeliveredAt?.Date == DateTime.UtcNow.Date);
        decimal codCollected = assignments.Where(da => da.IsCodCollected).Sum(da => da.CodAmount);
        decimal codPendingReconcile = assignments.Where(da => da.IsCodCollected && !da.CodReconciled).Sum(da => da.CodAmount);

        var orders = assignments.Select(da => OrderService.MapOrderToResponse(da.Order!)).ToList();
        var availableOrdersRes = await GetReadyForPickupPoolAsync();
        var availableOrders = availableOrdersRes.Data ?? new List<OrderResponse>();

        var dto = new ShipperDashboardDto
        {
            TotalAssignedOrders = totalAssigned,
            InTransitOrders = inTransit,
            DeliveredToday = deliveredToday,
            AvailableOrdersCount = availableOrders.Count,
            TotalCodCollected = codCollected,
            TotalCodPendingReconcile = codPendingReconcile,
            Orders = orders,
            AvailableOrders = availableOrders
        };

        return ApiResponse<ShipperDashboardDto>.Ok(dto);
    }

    public async Task<ApiResponse<List<OrderResponse>>> GetReadyForPickupPoolAsync()
    {
        var readyOrders = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.DeliveryAssignment)
            .Include(o => o.StatusHistories)
            .Include(o => o.PaymentTransactions)
            .Where(o => o.Status == OrderWorkflow.StatusReadyForPickup && o.DeliveryAssignment == null)
            .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
            .ToListAsync();

        var mapped = readyOrders.Select(OrderService.MapOrderToResponse).ToList();
        return ApiResponse<List<OrderResponse>>.Ok(mapped);
    }

    public async Task<ApiResponse<bool>> ClaimOrderForDeliveryAsync(int shipperId, int orderId, string? notes = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shipper = await _context.Users.FirstOrDefaultAsync(u => u.Id == shipperId && u.Role == "DeliveryStaff" && u.IsActive);
            if (shipper == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy tài khoản shipper đang hoạt động.");
            }

            var order = await _context.Orders
                .Include(o => o.DeliveryAssignment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng.");
            }

            if (order.DeliveryAssignment != null && order.DeliveryAssignment.ShipperId != shipperId)
            {
                return ApiResponse<bool>.Fail("FORBIDDEN: Đơn hàng này đã được nhận hoặc phân công cho shipper khác.");
            }

            if (order.Status != OrderWorkflow.StatusReadyForPickup && order.Status != OrderWorkflow.StatusDeliveryFailed)
            {
                return ApiResponse<bool>.Fail($"Đơn hàng chưa sẵn sàng để nhận giao (trạng thái hiện tại: {order.Status}).");
            }

            var now = DateTime.UtcNow;
            bool isCod = OrderWorkflow.IsCod(order.PaymentMethod);

            if (order.DeliveryAssignment == null)
            {
                order.DeliveryAssignment = new DeliveryAssignment
                {
                    OrderId = order.Id,
                    ShipperId = shipperId,
                    AssignedAt = now,
                    PickedUpAt = now,
                    Status = "InTransit",
                    CodAmount = isCod ? order.TotalAmount : 0,
                    Notes = string.IsNullOrWhiteSpace(notes) ? "Shipper tự nhận đơn sẵn sàng giao" : notes.Trim()
                };
                _context.DeliveryAssignments.Add(order.DeliveryAssignment);
            }
            else
            {
                order.DeliveryAssignment.Status = "InTransit";
                order.DeliveryAssignment.PickedUpAt ??= now;
                order.DeliveryAssignment.FailureReason = string.Empty;
            }

            order.Status = OrderWorkflow.StatusDelivering;
            order.UpdatedAt = now;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderWorkflow.StatusDelivering,
                Note = $"Shipper {shipper.FullName} ({shipper.PhoneNumber}) đã nhận đơn và bắt đầu giao hàng.",
                ChangedByUserId = shipper.Id,
                ChangedByName = shipper.FullName,
                ChangedByRole = "DeliveryStaff",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponse<bool>.Ok(true, $"Đã nhận đơn #{order.OrderCode} và chuyển sang Đang giao hàng!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi khi nhận đơn giao hàng: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<OrderResponse>>> GetAssignedOrdersAsync(int shipperId)
    {
        var assignments = await _context.DeliveryAssignments
            .Include(da => da.Order)
                .ThenInclude(o => o!.OrderDetails)
                    .ThenInclude(od => od.Options)
            .Include(da => da.Order)
                .ThenInclude(o => o!.OrderDetails)
                    .ThenInclude(od => od.Product)
            .Include(da => da.Order)
                .ThenInclude(o => o!.StatusHistories)
            .Include(da => da.Order)
                .ThenInclude(o => o!.PaymentTransactions)
            .Include(da => da.Shipper)
            .Where(da => da.ShipperId == shipperId && da.Order != null)
            .OrderByDescending(da => da.AssignedAt)
            .ToListAsync();

        var orders = assignments.Select(da => OrderService.MapOrderToResponse(da.Order!)).ToList();

        return ApiResponse<List<OrderResponse>>.Ok(orders);
    }

    public async Task<ApiResponse<bool>> AssignShipperAsync(
        AssignShipperRequest request,
        int? actorUserId = null,
        string? actorName = null,
        string? actorRole = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.DeliveryAssignment)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy đơn hàng.");
            }

            if (OrderWorkflow.IsTerminalStatus(order.Status))
            {
                return ApiResponse<bool>.Fail($"Không thể phân công shipper cho đơn hàng đã ở trạng thái '{order.Status}'.");
            }

            if (order.Status is not (OrderWorkflow.StatusConfirmed or OrderWorkflow.StatusPreparing or OrderWorkflow.StatusReadyForPickup or OrderWorkflow.StatusDeliveryFailed))
            {
                return ApiResponse<bool>.Fail($"Đơn hàng phải được xác nhận / chuẩn bị xong trước khi phân công shipper (trạng thái hiện tại: {order.Status}).");
            }

            var shipper = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ShipperId && u.Role == "DeliveryStaff" && u.IsActive);
            if (shipper == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy tài khoản nhân viên giao hàng (Shipper) đang hoạt động.");
            }

            var now = DateTime.UtcNow;
            bool isCod = OrderWorkflow.IsCod(order.PaymentMethod);

            if (order.DeliveryAssignment == null)
            {
                order.DeliveryAssignment = new DeliveryAssignment
                {
                    OrderId = order.Id,
                    ShipperId = shipper.Id,
                    AssignedAt = now,
                    Status = "Assigned",
                    CodAmount = isCod ? order.TotalAmount : 0,
                    Notes = request.Notes ?? string.Empty
                };
                _context.DeliveryAssignments.Add(order.DeliveryAssignment);
            }
            else
            {
                if (order.DeliveryAssignment.Status == "Delivered")
                {
                    return ApiResponse<bool>.Fail("Đơn hàng này đã được giao thành công trước đó.");
                }
                order.DeliveryAssignment.ShipperId = shipper.Id;
                order.DeliveryAssignment.AssignedAt = now;
                order.DeliveryAssignment.Status = "Assigned";
                order.DeliveryAssignment.FailureReason = string.Empty;
                order.DeliveryAssignment.CodAmount = isCod ? order.TotalAmount : 0;
                order.DeliveryAssignment.Notes = request.Notes ?? string.Empty;
            }

            order.Status = OrderWorkflow.StatusReadyForPickup;
            order.UpdatedAt = now;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderWorkflow.StatusReadyForPickup,
                Note = $"Đơn sẵn sàng giao — đã phân công shipper {shipper.FullName} ({shipper.PhoneNumber}).",
                ChangedByUserId = actorUserId,
                ChangedByName = actorName,
                ChangedByRole = actorRole ?? "Staff",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponse<bool>.Ok(true, $"Phân công đơn {order.OrderCode} cho {shipper.FullName} thành công!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi phân công shipper: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> UpdateDeliveryStatusAsync(int shipperId, int orderId, UpdateDeliveryStatusRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var assignment = await _context.DeliveryAssignments
                .Include(da => da.Order)
                    .ThenInclude(o => o!.PaymentTransactions)
                .Include(da => da.Shipper)
                .FirstOrDefaultAsync(da => da.OrderId == orderId);

            if (assignment == null || assignment.Order == null)
            {
                return ApiResponse<bool>.Fail("Không tìm thấy thông tin giao hàng cho đơn này.");
            }

            if (assignment.ShipperId != shipperId)
            {
                return ApiResponse<bool>.Fail("FORBIDDEN: Chỉ shipper được phân công mới có quyền cập nhật trạng thái giao hàng của đơn này.");
            }

            var order = assignment.Order;
            if (OrderWorkflow.IsTerminalStatus(order.Status))
            {
                return ApiResponse<bool>.Fail($"Đơn hàng đã ở trạng thái kết thúc ({order.Status}), không thể cập nhật thêm.");
            }

            var targetStatus = OrderWorkflow.NormalizeDeliveryTargetStatus(request.Status);
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes)) assignment.Notes = request.Notes.Trim();
            if (!string.IsNullOrWhiteSpace(request.ProofImageUrl)) assignment.ProofImageUrl = request.ProofImageUrl.Trim();

            if (targetStatus is "PickedUp" or "InTransit")
            {
                if (assignment.Status == "Delivered")
                {
                    return ApiResponse<bool>.Fail("Đơn hàng đã được giao thành công trước đó.");
                }

                if (order.Status is not (OrderWorkflow.StatusReadyForPickup or OrderWorkflow.StatusConfirmed or OrderWorkflow.StatusPreparing or OrderWorkflow.StatusDeliveryFailed or OrderWorkflow.StatusDelivering))
                {
                    return ApiResponse<bool>.Fail($"Không thể chuyển đơn sang Đang giao hàng từ trạng thái '{order.Status}'.");
                }

                assignment.Status = targetStatus;
                assignment.PickedUpAt ??= now;
                assignment.FailureReason = string.Empty;

                order.Status = OrderWorkflow.StatusDelivering;
                order.UpdatedAt = now;

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = assignment.OrderId,
                    Status = OrderWorkflow.StatusDelivering,
                    Note = $"Shipper {assignment.Shipper?.FullName ?? ""} đang giao hàng tới khách. {request.Notes}".Trim(),
                    ChangedByUserId = shipperId,
                    ChangedByName = assignment.Shipper?.FullName,
                    ChangedByRole = "DeliveryStaff",
                    CreatedAt = now
                });
            }
            else if (targetStatus == OrderWorkflow.StatusDelivered)
            {
                if (order.Status != OrderWorkflow.StatusDelivering)
                {
                    return ApiResponse<bool>.Fail("Đơn hàng phải đang ở trạng thái Đang giao hàng (Delivering) trước khi xác nhận giao thành công.");
                }

                if (assignment.Status == "Delivered" || assignment.IsCodCollected || (OrderWorkflow.IsCod(order.PaymentMethod) && OrderWorkflow.IsPaid(order.PaymentStatus)))
                {
                    return ApiResponse<bool>.Fail("Đơn hàng đã được giao và xác nhận thanh toán trước đó, không thể xác nhận hai lần.");
                }

                bool isCod = OrderWorkflow.IsCod(order.PaymentMethod);
                if (isCod)
                {
                    if (!request.IsCodCollected)
                    {
                        return ApiResponse<bool>.Fail("Đơn hàng COD bắt buộc shipper phải xác nhận đã thu đủ tiền mặt từ khách hàng trước khi hoàn tất giao hàng.");
                    }

                    assignment.IsCodCollected = true;
                    assignment.CodAmount = order.TotalAmount;
                    assignment.CodCollectedAt = now;
                    assignment.CollectedByUserId = shipperId;

                    order.PaymentStatus = OrderWorkflow.PaymentPaid;
                    order.PaidAt = now;
                    order.PaymentConfirmedByUserId = shipperId;
                    order.PaymentConfirmedByName = assignment.Shipper?.FullName ?? $"Shipper #{shipperId}";

                    var txUnique = $"COD_{order.OrderCode}_{Guid.NewGuid():N}"[..Math.Min(64, $"COD_{order.OrderCode}_{Guid.NewGuid():N}".Length)];
                    _context.PaymentTransactions.Add(new PaymentTransaction
                    {
                        OrderId = order.Id,
                        UserId = order.UserId,
                        MomoOrderId = txUnique,
                        RequestId = txUnique,
                        TransactionId = $"COD_CASH_{shipperId}_{now:yyyyMMddHHmmss}",
                        PaymentMethod = "COD",
                        Amount = order.TotalAmount,
                        PaymentStatus = OrderWorkflow.PaymentSuccess,
                        CreatedAt = now,
                        PaymentExpiresAt = now,
                        PaidAt = now,
                        TransactionResponse = JsonSerializer.Serialize(new
                        {
                            collectedByShipperId = shipperId,
                            collectedByShipperName = assignment.Shipper?.FullName,
                            collectedAt = now,
                            notes = request.Notes
                        })
                    });
                }

                assignment.Status = "Delivered";
                assignment.DeliveredAt = now;
                order.Status = OrderWorkflow.StatusDelivered;
                order.UpdatedAt = now;

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = assignment.OrderId,
                    Status = OrderWorkflow.StatusDelivered,
                    Note = isCod
                        ? $"Shipper {assignment.Shipper?.FullName ?? ""} giao hàng thành công và xác nhận đã thu đủ {order.TotalAmount:N0}đ tiền mặt COD."
                        : $"Shipper {assignment.Shipper?.FullName ?? ""} đã giao hàng thành công tới khách hàng.",
                    ChangedByUserId = shipperId,
                    ChangedByName = assignment.Shipper?.FullName,
                    ChangedByRole = "DeliveryStaff",
                    CreatedAt = now
                });
            }
            else if (targetStatus is OrderWorkflow.StatusDeliveryFailed or OrderWorkflow.StatusRejected)
            {
                if (order.Status != OrderWorkflow.StatusDelivering)
                {
                    return ApiResponse<bool>.Fail("Chỉ có thể báo giao thất bại / từ chối nhận khi đơn đang trong quá trình giao hàng.");
                }

                if (request.IsCodCollected)
                {
                    return ApiResponse<bool>.Fail("Không thể ghi nhận đã thu tiền COD khi giao hàng thất bại hoặc khách từ chối nhận.");
                }

                if (string.IsNullOrWhiteSpace(request.FailureReason))
                {
                    return ApiResponse<bool>.Fail("Vui lòng nhập rõ lý do giao hàng thất bại hoặc từ chối nhận.");
                }

                bool isRejected = targetStatus == OrderWorkflow.StatusRejected;
                assignment.Status = isRejected ? "Rejected" : "Failed";
                assignment.FailureReason = request.FailureReason.Trim();
                assignment.IsCodCollected = false;

                order.Status = isRejected ? OrderWorkflow.StatusRejected : OrderWorkflow.StatusDeliveryFailed;
                order.UpdatedAt = now;
                // Do NOT modify PaymentStatus to Paid when delivery fails/is rejected

                _context.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = assignment.OrderId,
                    Status = order.Status,
                    Note = $"{(isRejected ? "Khách từ chối nhận hàng" : "Giao hàng thất bại")}: {assignment.FailureReason}",
                    ChangedByUserId = shipperId,
                    ChangedByName = assignment.Shipper?.FullName,
                    ChangedByRole = "DeliveryStaff",
                    CreatedAt = now
                });
            }
            else
            {
                return ApiResponse<bool>.Fail($"Trạng thái giao hàng '{request.Status}' không hợp lệ.");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponse<bool>.Ok(true, "Cập nhật tiến độ giao hàng thành công!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<bool>.Fail($"Lỗi cập nhật tiến độ giao hàng: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ReconcileCodAsync(ReconcileCodRequest request)
    {
        if (request.OrderIds == null || !request.OrderIds.Any())
        {
            return ApiResponse<bool>.Fail("Vui lòng chọn danh sách đơn cần đối soát COD.");
        }

        var assignments = await _context.DeliveryAssignments
            .Where(da => request.OrderIds.Contains(da.OrderId) && da.IsCodCollected && !da.CodReconciled)
            .ToListAsync();

        if (!assignments.Any())
        {
            return ApiResponse<bool>.Fail("Không có đơn COD hợp lệ (đã thu tiền và chưa đối soát) để đối soát.");
        }

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
