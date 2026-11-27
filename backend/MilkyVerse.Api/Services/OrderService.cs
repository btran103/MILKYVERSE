using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IOrderService
{
    Task<ApiResponse<OrderResponse>> CreateOrderAsync(int? userId, CreateOrderRequest request);
    Task<ApiResponse<List<OrderResponse>>> GetOrdersAsync(int? userId = null);
    Task<ApiResponse<OrderResponse>> GetOrderByIdAsync(int orderId, int? userId = null);
    Task<ApiResponse<OrderResponse>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, int? actorUserId = null, string? actorRole = null, string? actorName = null);
    Task<ApiResponse<OrderResponse>> CancelOrderAsync(int orderId, int? userId, string reason, string? actorRole = null, string? actorName = null);
    Task<ApiResponse<ValidatePromoResponse>> ValidatePromotionAsync(ValidatePromoRequest request);
    Task<int> ExpireOverdueOnlinePaymentsAsync(CancellationToken ct = default);
}

public class OrderService : IOrderService
{
    private readonly MilkyVerseDbContext _context;

    public OrderService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<OrderResponse>> CreateOrderAsync(int? userId, CreateOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return ApiResponse<OrderResponse>.Fail("Giỏ hàng của bạn đang trống.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.CustomerPhone) || string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            return ApiResponse<OrderResponse>.Fail("Vui lòng cung cấp đầy đủ họ tên, số điện thoại và địa chỉ giao hàng.");
        }

        if (userId.HasValue)
        {
            var orderUser = await _context.Users.FindAsync(userId.Value);
            if (orderUser != null && orderUser.Role != "Customer" && orderUser.Role != "Staff")
            {
                return ApiResponse<OrderResponse>.Fail($"Tài khoản vai trò '{orderUser.Role}' không có quyền đặt món. Chỉ tài khoản Khách hàng và Nhân viên mới có quyền đặt món.");
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            decimal subtotal = 0;
            var orderDetailsToCreate = new List<OrderDetail>();
            var allToppings = await _context.Toppings.ToListAsync();

            foreach (var item in request.Items)
            {
                decimal itemUnitPrice = 0;
                string itemName = item.ProductName;
                string itemType = string.IsNullOrWhiteSpace(item.ItemType) ? "DRINK" : item.ItemType;
                int qty = Math.Max(1, item.Quantity);

                if (item.IsCustomDIY)
                {
                    decimal baseDiy = 40000;
                    if (item.Size == "L") baseDiy += 8000;
                    else if (item.Size == "S") baseDiy = Math.Max(10000, baseDiy - 5000);
                    itemUnitPrice = baseDiy;
                    itemName = string.IsNullOrWhiteSpace(item.ProductName) ? "Ly Tự Thiết Kế (DIY Studio)" : item.ProductName;
                    itemType = "DRINK";
                }
                else
                {
                    Product? product = null;
                    if (item.ProductId.HasValue)
                    {
                        product = await _context.Products.FindAsync(item.ProductId.Value);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.ProductName))
                    {
                        product = await _context.Products.FirstOrDefaultAsync(p => p.Name.ToLower() == item.ProductName.Trim().ToLower() && p.IsActive);
                    }

                    if (product == null || !product.IsActive)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<OrderResponse>.Fail($"Món '{item.ProductName}' hiện không khả dụng.");
                    }

                    itemUnitPrice = product.BasePrice;
                    itemType = product.ProductType;
                    itemName = product.Name;

                    if (itemType == "DRINK")
                    {
                        if (item.Size == "L") itemUnitPrice += 8000;
                        else if (item.Size == "S") itemUnitPrice = Math.Max(10000, itemUnitPrice - 5000);
                    }
                    else if (itemType == "FOOD")
                    {
                        if (item.PortionSize != null && item.PortionSize.Contains("Phần lớn"))
                        {
                            itemUnitPrice += 15000;
                        }
                        else if (item.PortionSize != null && (item.PortionSize.Contains("khổng lồ") || item.PortionSize.Contains("gia đình")))
                        {
                            itemUnitPrice += 35000;
                        }
                    }
                }

                // Process Toppings / Extra additions
                decimal itemToppingPrice = 0;
                var itemOptions = new List<OrderItemOption>();

                if (item.Toppings != null && item.Toppings.Any())
                {
                    foreach (var topReq in item.Toppings)
                    {
                        var topping = allToppings.FirstOrDefault(t => t.Id == topReq.ToppingId && t.IsAvailable);
                        if (topping == null)
                        {
                            await transaction.RollbackAsync();
                            return ApiResponse<OrderResponse>.Fail("Một trong các topping / món thêm hiện không có sẵn.");
                        }

                        int topQty = Math.Max(1, topReq.Quantity);
                        itemToppingPrice += topping.Price * topQty;

                        itemOptions.Add(new OrderItemOption
                        {
                            ToppingId = topping.Id,
                            ToppingName = topping.Name,
                            Price = topping.Price,
                            Quantity = topQty
                        });
                    }
                }

                decimal finalItemUnitPrice = itemUnitPrice + itemToppingPrice;
                decimal itemTotal = finalItemUnitPrice * qty;
                subtotal += itemTotal;

                var detail = new OrderDetail
                {
                    ProductId = item.ProductId,
                    ProductName = itemName,
                    ItemType = itemType,
                    Size = item.Size ?? "Standard",
                    SugarLevel = item.SugarLevel ?? "100%",
                    IceLevel = item.IceLevel ?? "100%",
                    SpicyLevel = item.SpicyLevel ?? "Không cay",
                    DippingSauce = item.DippingSauce ?? "Tương ớt",
                    PortionSize = item.PortionSize ?? "Tiêu chuẩn",
                    UnitPrice = finalItemUnitPrice,
                    Quantity = qty,
                    TotalPrice = itemTotal,
                    IsCustomDIY = item.IsCustomDIY,
                    CustomNotes = item.CustomNotes ?? string.Empty,
                    Options = itemOptions
                };

                orderDetailsToCreate.Add(detail);
            }

            // Calculate Discount
            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(request.PromotionCode))
            {
                var promoRes = await ValidatePromotionAsync(new ValidatePromoRequest
                {
                    Code = request.PromotionCode.Trim().ToUpper(),
                    OrderSubtotal = subtotal
                });

                if (promoRes.Success && promoRes.Data != null && promoRes.Data.IsValid)
                {
                    discount = promoRes.Data.DiscountAmount;
                    var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == request.PromotionCode.Trim().ToUpper());
                    if (promo != null)
                    {
                        promo.UsedCount++;
                    }
                }
            }

            decimal shippingFee = subtotal >= 200000 ? 0 : 15000;
            decimal totalAmount = Math.Max(0, subtotal - discount + shippingFee);

            var orderCode = "MV" + DateTime.UtcNow.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N")[..6].ToUpper();
            var nowUtc = DateTime.UtcNow;
            var paymentMethodNormalized = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "COD" : request.PaymentMethod.Trim().ToUpper();
            bool isCod = OrderWorkflow.IsCod(paymentMethodNormalized);

            // COD never has an online payment expiry timer and starts with PendingConfirmation + Unpaid.
            DateTime? paymentExpiresAt = isCod
                ? null
                : (paymentMethodNormalized == "SEPAY" ? nowUtc.AddMinutes(15) : nowUtc.AddSeconds(120));

            var initialStatus = isCod ? OrderWorkflow.StatusPendingConfirmation : OrderWorkflow.StatusPending;
            var initialPaymentStatus = isCod ? OrderWorkflow.PaymentUnpaid : OrderWorkflow.PaymentPending;

            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                Subtotal = subtotal,
                DiscountAmount = discount,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                Status = initialStatus,
                PaymentStatus = initialPaymentStatus,
                PaymentMethod = paymentMethodNormalized,
                PaymentExpiresAt = paymentExpiresAt,
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                ShippingAddress = request.ShippingAddress.Trim(),
                Note = request.Note ?? string.Empty,
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc,
                OrderDetails = orderDetailsToCreate
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Record initial history
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = initialStatus,
                Note = isCod
                    ? "Đặt hàng COD thành công. Chờ cửa hàng xác nhận (thanh toán tiền mặt khi nhận hàng)."
                    : $"Đơn hàng mới được tạo thành công ({paymentMethodNormalized}). Thời hạn thanh toán {(paymentMethodNormalized == "SEPAY" ? "15:00" : "02:00")} (hết hạn lúc {paymentExpiresAt!.Value:HH:mm:ss} UTC).",
                ChangedByUserId = userId,
                ChangedByName = request.CustomerName.Trim(),
                ChangedByRole = userId.HasValue ? "Customer" : "Guest",
                CreatedAt = nowUtc
            });

            // Auto deduct inventory for recipes
            foreach (var detail in order.OrderDetails)
            {
                if (detail.ProductId.HasValue)
                {
                    var recipe = await _context.Recipes
                        .Include(r => r.RecipeIngredients)
                        .FirstOrDefaultAsync(r => r.ProductId == detail.ProductId.Value && r.Size == detail.Size);

                    if (recipe != null)
                    {
                        foreach (var ri in recipe.RecipeIngredients)
                        {
                            var ing = await _context.Ingredients.FindAsync(ri.IngredientId);
                            if (ing != null)
                            {
                                decimal deductQty = ri.Amount * detail.Quantity;
                                ing.CurrentStock = Math.Max(0, ing.CurrentStock - deductQty);

                                _context.InventoryTransactions.Add(new InventoryTransaction
                                {
                                    IngredientId = ing.Id,
                                    TransactionType = "ORDER_DEDUCT",
                                    Quantity = -deductQty,
                                    BalanceAfter = ing.CurrentStock,
                                    ReferenceType = "Order",
                                    ReferenceId = order.OrderCode,
                                    Note = $"Trừ kho cho đơn {order.OrderCode} ({detail.ProductName} x{detail.Quantity})",
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = await GetOrderByIdAsync(order.Id);
            return ApiResponse<OrderResponse>.Ok(response.Data!, "Đặt món thành công! MilkyVerse đang chuẩn bị đơn cho bạn.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<OrderResponse>.Fail($"Đã xảy ra lỗi khi tạo đơn hàng: {ex.Message}");
        }
    }

    public async Task<int> ExpireOverdueOnlinePaymentsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var overdueOnlineOrders = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .Where(o => o.PaymentMethod != "COD"
                     && o.PaymentStatus == OrderWorkflow.PaymentPending
                     && o.PaymentExpiresAt != null
                     && o.PaymentExpiresAt <= now
                     && o.Status != OrderWorkflow.StatusCancelled
                     && o.Status != OrderWorkflow.StatusDelivered
                     && o.Status != OrderWorkflow.StatusCompleted
                     && o.Status != OrderWorkflow.StatusRejected)
            .ToListAsync(ct);

        if (overdueOnlineOrders.Count == 0) return 0;

        foreach (var order in overdueOnlineOrders)
        {
            order.PaymentStatus = OrderWorkflow.PaymentExpired;
            order.UpdatedAt = now;

            foreach (var pt in order.PaymentTransactions.Where(t => t.PaymentStatus == OrderWorkflow.PaymentPending && t.PaymentExpiresAt <= now))
            {
                pt.PaymentStatus = OrderWorkflow.PaymentExpired;
            }

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                Note = $"Phiên thanh toán online ({order.PaymentMethod}) đã hết hạn thanh toán.",
                ChangedByRole = "System",
                ChangedByName = "OnlinePaymentExpiration",
                CreatedAt = now
            });
        }

        await _context.SaveChangesAsync(ct);
        return overdueOnlineOrders.Count;
    }

    public async Task<ApiResponse<List<OrderResponse>>> GetOrdersAsync(int? userId = null)
    {
        await ExpireOverdueOnlinePaymentsAsync();

        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.DeliveryAssignment)
                .ThenInclude(da => da!.Shipper)
            .Include(o => o.StatusHistories)
            .Include(o => o.PaymentTransactions)
            .AsQueryable();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapOrderToResponse(o))
            .ToListAsync();

        return ApiResponse<List<OrderResponse>>.Ok(orders);
    }

    public async Task<ApiResponse<OrderResponse>> GetOrderByIdAsync(int orderId, int? userId = null)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.DeliveryAssignment)
                .ThenInclude(da => da!.Shipper)
            .Include(o => o.StatusHistories)
            .Include(o => o.PaymentTransactions)
            .AsQueryable();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
        {
            return ApiResponse<OrderResponse>.Fail("Không tìm thấy đơn hàng.");
        }

        // Auto-expire ONLY online payment sessions if PaymentExpiresAt elapsed while still Pending.
        // COD orders are explicitly excluded from automatic payment expiration or cancellation.
        var now = DateTime.UtcNow;
        bool changed = false;
        if (OrderWorkflow.IsOnlinePayment(order.PaymentMethod)
            && order.PaymentStatus == OrderWorkflow.PaymentPending
            && order.PaymentExpiresAt.HasValue
            && now >= order.PaymentExpiresAt.Value)
        {
            order.PaymentStatus = OrderWorkflow.PaymentExpired;
            order.UpdatedAt = now;
            changed = true;
        }
        if (OrderWorkflow.IsOnlinePayment(order.PaymentMethod))
        {
            foreach (var pt in order.PaymentTransactions.Where(t => t.PaymentStatus == OrderWorkflow.PaymentPending && now >= t.PaymentExpiresAt))
            {
                pt.PaymentStatus = OrderWorkflow.PaymentExpired;
                changed = true;
            }
        }
        if (changed)
        {
            await _context.SaveChangesAsync();
        }

        return ApiResponse<OrderResponse>.Ok(MapOrderToResponse(order));
    }

    public async Task<ApiResponse<OrderResponse>> UpdateOrderStatusAsync(
        int orderId,
        UpdateOrderStatusRequest request,
        int? actorUserId = null,
        string? actorRole = null,
        string? actorName = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Options)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.DeliveryAssignment)
                    .ThenInclude(da => da!.Shipper)
                .Include(o => o.StatusHistories)
                .Include(o => o.PaymentTransactions)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ApiResponse<OrderResponse>.Fail("Không tìm thấy đơn hàng.");
            }

            if (string.Equals(actorRole, "DeliveryStaff", StringComparison.OrdinalIgnoreCase))
            {
                if (!actorUserId.HasValue || order.DeliveryAssignment == null || order.DeliveryAssignment.ShipperId != actorUserId.Value)
                {
                    return ApiResponse<OrderResponse>.Fail("FORBIDDEN: Shipper chỉ được cập nhật đơn hàng đã được phân công cho mình qua quy trình giao hàng.");
                }
            }

            var targetStatus = !string.IsNullOrWhiteSpace(request.Status) ? request.Status.Trim() : order.Status;
            if (!OrderWorkflow.CanTransition(order.Status, targetStatus))
            {
                return ApiResponse<OrderResponse>.Fail($"Không thể chuyển trạng thái đơn hàng từ '{order.Status}' sang '{targetStatus}'.");
            }

            var now = DateTime.UtcNow;
            bool isCod = OrderWorkflow.IsCod(order.PaymentMethod);
            var targetPaymentStatus = !string.IsNullOrWhiteSpace(request.PaymentStatus) ? request.PaymentStatus.Trim() : null;

            if (targetPaymentStatus != null)
            {
                if (isCod)
                {
                    if (string.Equals(targetPaymentStatus, OrderWorkflow.PaymentExpired, StringComparison.OrdinalIgnoreCase))
                    {
                        return ApiResponse<OrderResponse>.Fail("Đơn hàng COD không áp dụng trạng thái hết hạn thanh toán online.");
                    }

                    if (OrderWorkflow.IsPaid(targetPaymentStatus))
                    {
                        if (OrderWorkflow.IsPaid(order.PaymentStatus))
                        {
                            return ApiResponse<OrderResponse>.Fail("Đơn hàng COD này đã được xác nhận thanh toán trước đó, không thể xác nhận hai lần.");
                        }

                        bool isDeliveringCompletion = string.Equals(targetStatus, OrderWorkflow.StatusDelivered, StringComparison.OrdinalIgnoreCase)
                                                      || string.Equals(targetStatus, OrderWorkflow.StatusCompleted, StringComparison.OrdinalIgnoreCase);

                        if (!isDeliveringCompletion)
                        {
                            return ApiResponse<OrderResponse>.Fail("Đơn hàng COD giữ nguyên trạng thái Unpaid khi xử lý ở quầy/bếp và chỉ chuyển sang Paid khi đã giao hàng thành công.");
                        }

                        if (order.DeliveryAssignment != null && !order.DeliveryAssignment.IsCodCollected && string.Equals(actorRole, "DeliveryStaff", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            // Ensure shipper/delivery collection is tracked even if completed by admin after delivery
                            order.DeliveryAssignment.IsCodCollected = true;
                            order.DeliveryAssignment.CodCollectedAt ??= now;
                            order.DeliveryAssignment.CollectedByUserId ??= actorUserId;
                        }
                    }
                }
                else if (OrderWorkflow.IsPaid(targetPaymentStatus) && OrderWorkflow.IsPaid(order.PaymentStatus))
                {
                    return ApiResponse<OrderResponse>.Fail("Đơn hàng đã được xác nhận thanh toán trước đó, không thể xác nhận hai lần.");
                }

                order.PaymentStatus = targetPaymentStatus;
                if (OrderWorkflow.IsPaid(targetPaymentStatus))
                {
                    order.PaidAt ??= now;
                    order.PaymentConfirmedByUserId ??= actorUserId;
                    order.PaymentConfirmedByName ??= actorName ?? actorRole ?? "Staff";
                }
            }

            order.Status = targetStatus;
            order.UpdatedAt = now;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                Note = string.IsNullOrWhiteSpace(request.Note)
                    ? $"Trạng thái đơn hàng cập nhật thành: {order.Status}"
                    : request.Note.Trim(),
                ChangedByUserId = actorUserId,
                ChangedByName = actorName,
                ChangedByRole = actorRole,
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<OrderResponse>.Ok(MapOrderToResponse(order), "Cập nhật trạng thái đơn thành công!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<OrderResponse>.Fail($"Lỗi cập nhật trạng thái đơn hàng: {ex.Message}");
        }
    }

    public async Task<ApiResponse<OrderResponse>> CancelOrderAsync(
        int orderId,
        int? userId,
        string reason,
        string? actorRole = null,
        string? actorName = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Options)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.DeliveryAssignment)
                    .ThenInclude(da => da!.Shipper)
                .Include(o => o.StatusHistories)
                .Include(o => o.PaymentTransactions)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ApiResponse<OrderResponse>.Fail("Không tìm thấy đơn hàng.");
            }

            if (OrderWorkflow.IsTerminalStatus(order.Status))
            {
                return ApiResponse<OrderResponse>.Fail("Đơn hàng đã hoàn tất hoặc đã hủy trước đó, không thể hủy.");
            }

            bool isPrivileged = string.Equals(actorRole, "Admin", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(actorRole, "Manager", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(actorRole, "Staff", StringComparison.OrdinalIgnoreCase);

            if (!isPrivileged)
            {
                if (order.UserId.HasValue && (!userId.HasValue || order.UserId.Value != userId.Value))
                {
                    return ApiResponse<OrderResponse>.Fail("FORBIDDEN: Bạn không có quyền hủy đơn hàng của người khác.");
                }

                if (!OrderWorkflow.IsCustomerCancellableStatus(order.Status))
                {
                    return ApiResponse<OrderResponse>.Fail("Đơn hàng đang được chuẩn bị hoặc đang giao hàng, không thể tự hủy. Vui lòng chat với Quản lý để được hỗ trợ.");
                }
            }

            var now = DateTime.UtcNow;
            var cancelNote = string.IsNullOrWhiteSpace(reason)
                ? (isPrivileged ? "Nhân viên cửa hàng hủy đơn theo quy định" : "Khách hàng hủy đơn theo yêu cầu")
                : reason.Trim();

            order.Status = OrderWorkflow.StatusCancelled;
            order.CancelReason = cancelNote;
            order.UpdatedAt = now;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderWorkflow.StatusCancelled,
                Note = cancelNote,
                ChangedByUserId = userId,
                ChangedByName = actorName ?? (isPrivileged ? "Nhân viên" : order.CustomerName),
                ChangedByRole = actorRole ?? "Customer",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<OrderResponse>.Ok(MapOrderToResponse(order), "Đã hủy đơn hàng thành công!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<OrderResponse>.Fail($"Lỗi hủy đơn hàng: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ValidatePromoResponse>> ValidatePromotionAsync(ValidatePromoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResponse<ValidatePromoResponse>.Fail("Vui lòng nhập mã ưu đãi.");
        }

        var rawCode = request.Code.Trim().ToUpper();
        var normalizedCode = rawCode.TrimStart('#');
        var now = DateTime.UtcNow;

        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => (p.Code == rawCode || p.Code == normalizedCode || p.Code == "#" + normalizedCode) && p.IsActive);

        if (promo == null)
        {
            // Support random / customer generated promo code like #MK2095F54 or MK...
            if (rawCode.StartsWith("MK") || rawCode.StartsWith("#MK"))
            {
                var discountAmount = Math.Min(25000, request.OrderSubtotal > 30000 ? 25000 : 10000);
                return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
                {
                    IsValid = true,
                    Code = rawCode,
                    DiscountAmount = discountAmount,
                    Message = $"Áp dụng thành công mã ưu đãi {rawCode}: Giảm ngay {discountAmount:N0}đ!"
                });
            }

            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = rawCode,
                DiscountAmount = 0,
                Message = "Mã ưu đãi không tồn tại hoặc đã hết hạn."
            });
        }

        if (now < promo.StartDate || now > promo.EndDate)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = rawCode,
                DiscountAmount = 0,
                Message = "Mã ưu đãi đã hết thời gian áp dụng."
            });
        }

        if (promo.UsedCount >= promo.MaxUsage)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = rawCode,
                DiscountAmount = 0,
                Message = "Mã ưu đãi đã hết lượt sử dụng."
            });
        }

        if (request.OrderSubtotal < promo.MinOrderValue)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = rawCode,
                DiscountAmount = 0,
                Message = $"Mã ưu đãi chỉ áp dụng cho đơn hàng từ {promo.MinOrderValue:N0}đ."
            });
        }

        decimal discount = 0;
        if (promo.DiscountPercent > 0)
        {
            discount = Math.Round(request.OrderSubtotal * (promo.DiscountPercent / 100m));
        }
        else if (promo.DiscountAmount > 0)
        {
            discount = promo.DiscountAmount;
        }

        discount = Math.Min(discount, request.OrderSubtotal);

        return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
        {
            IsValid = true,
            Code = rawCode,
            DiscountAmount = discount,
            Message = $"Áp dụng thành công mã {rawCode}: Giảm {discount:N0}đ"
        });
    }

    public static OrderResponse MapOrderToResponse(Order o)
    {
        var now = DateTime.UtcNow;
        bool isCod = OrderWorkflow.IsCod(o.PaymentMethod);
        var canCancel = OrderWorkflow.IsCustomerCancellableStatus(o.Status);

        DateTime? activeExpiresAt = isCod ? null : o.PaymentExpiresAt;
        var latestTx = !isCod
            ? o.PaymentTransactions?.OrderByDescending(pt => pt.CreatedAt).FirstOrDefault()
            : null;
        if (latestTx != null)
        {
            activeExpiresAt = latestTx.PaymentExpiresAt;
        }

        int remainingPaymentSecs = 0;
        if (!isCod && o.PaymentStatus == OrderWorkflow.PaymentPending && activeExpiresAt.HasValue)
        {
            remainingPaymentSecs = Math.Max(0, (int)Math.Ceiling((activeExpiresAt.Value - now).TotalSeconds));
        }

        var effectivePaymentStatus = o.PaymentStatus;
        if (!isCod && effectivePaymentStatus == OrderWorkflow.PaymentPending && activeExpiresAt.HasValue && remainingPaymentSecs <= 0)
        {
            effectivePaymentStatus = OrderWorkflow.PaymentExpired;
        }

        return new OrderResponse
        {
            Id = o.Id,
            OrderCode = o.OrderCode,
            UserId = o.UserId,
            Subtotal = o.Subtotal,
            DiscountAmount = o.DiscountAmount,
            ShippingFee = o.ShippingFee,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            PaymentStatus = effectivePaymentStatus,
            PaymentMethod = o.PaymentMethod,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            ShippingAddress = o.ShippingAddress,
            Note = o.Note,
            CancelReason = o.CancelReason,
            CanCancel = canCancel,
            CreatedAt = DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc),
            UpdatedAt = o.UpdatedAt.HasValue ? DateTime.SpecifyKind(o.UpdatedAt.Value, DateTimeKind.Utc) : null,
            PaymentExpiresAt = activeExpiresAt.HasValue ? DateTime.SpecifyKind(activeExpiresAt.Value, DateTimeKind.Utc) : null,
            PaidAt = o.PaidAt.HasValue ? DateTime.SpecifyKind(o.PaidAt.Value, DateTimeKind.Utc) : null,
            PaymentConfirmedByUserId = o.PaymentConfirmedByUserId,
            PaymentConfirmedByName = o.PaymentConfirmedByName ?? string.Empty,
            RemainingPaymentSeconds = remainingPaymentSecs,
            ServerTimeUtc = now,
            PaymentTransactions = o.PaymentTransactions != null
                ? o.PaymentTransactions.OrderByDescending(pt => pt.CreatedAt).Select(pt =>
                {
                    int rem = !isCod && pt.PaymentStatus == OrderWorkflow.PaymentPending
                        ? Math.Max(0, (int)Math.Ceiling((pt.PaymentExpiresAt - now).TotalSeconds))
                        : 0;
                    bool exp = !isCod && (pt.PaymentStatus == OrderWorkflow.PaymentExpired || (pt.PaymentStatus == OrderWorkflow.PaymentPending && rem <= 0));
                    return new PaymentTransactionDto
                    {
                        Id = pt.Id,
                        OrderId = pt.OrderId,
                        OrderCode = o.OrderCode,
                        MomoOrderId = pt.MomoOrderId,
                        RequestId = pt.RequestId,
                        TransactionId = pt.TransactionId,
                        PaymentMethod = pt.PaymentMethod,
                        Amount = pt.Amount,
                        PaymentStatus = exp && pt.PaymentStatus == OrderWorkflow.PaymentPending ? OrderWorkflow.PaymentExpired : pt.PaymentStatus,
                        PayUrl = pt.PayUrl,
                        QrCodeUrl = pt.QrCodeUrl,
                        Deeplink = pt.Deeplink,
                        CreatedAt = DateTime.SpecifyKind(pt.CreatedAt, DateTimeKind.Utc),
                        PaymentExpiresAt = DateTime.SpecifyKind(pt.PaymentExpiresAt, DateTimeKind.Utc),
                        PaidAt = pt.PaidAt.HasValue ? DateTime.SpecifyKind(pt.PaidAt.Value, DateTimeKind.Utc) : null,
                        RemainingSeconds = rem,
                        ServerTimeUtc = now,
                        IsExpired = exp
                    };
                }).ToList()
                : new List<PaymentTransactionDto>(),
            ShipperId = o.DeliveryAssignment != null ? o.DeliveryAssignment.ShipperId : null,
            ShipperName = o.DeliveryAssignment != null && o.DeliveryAssignment.Shipper != null ? o.DeliveryAssignment.Shipper.FullName : string.Empty,
            ShipperPhone = o.DeliveryAssignment != null && o.DeliveryAssignment.Shipper != null ? o.DeliveryAssignment.Shipper.PhoneNumber : string.Empty,
            DeliveryStatus = o.DeliveryAssignment != null ? o.DeliveryAssignment.Status : string.Empty,
            DeliveryFailureReason = o.DeliveryAssignment != null ? o.DeliveryAssignment.FailureReason : string.Empty,
            AssignedAt = o.DeliveryAssignment != null ? DateTime.SpecifyKind(o.DeliveryAssignment.AssignedAt, DateTimeKind.Utc) : null,
            PickedUpAt = o.DeliveryAssignment != null && o.DeliveryAssignment.PickedUpAt.HasValue ? DateTime.SpecifyKind(o.DeliveryAssignment.PickedUpAt.Value, DateTimeKind.Utc) : null,
            DeliveredAt = o.DeliveryAssignment != null && o.DeliveryAssignment.DeliveredAt.HasValue ? DateTime.SpecifyKind(o.DeliveryAssignment.DeliveredAt.Value, DateTimeKind.Utc) : null,
            CodAmount = o.DeliveryAssignment != null ? o.DeliveryAssignment.CodAmount : (isCod ? o.TotalAmount : 0),
            IsCodCollected = o.DeliveryAssignment != null && o.DeliveryAssignment.IsCodCollected,
            CodCollectedAt = o.DeliveryAssignment != null && o.DeliveryAssignment.CodCollectedAt.HasValue ? DateTime.SpecifyKind(o.DeliveryAssignment.CodCollectedAt.Value, DateTimeKind.Utc) : null,
            CodReconciled = o.DeliveryAssignment != null && o.DeliveryAssignment.CodReconciled,
            StatusHistories = o.StatusHistories != null ? o.StatusHistories.OrderBy(h => h.CreatedAt).Select(h => new OrderStatusHistoryDto
            {
                Id = h.Id,
                Status = h.Status,
                Note = h.Note,
                ChangedByUserId = h.ChangedByUserId,
                ChangedByName = h.ChangedByName,
                ChangedByRole = h.ChangedByRole,
                CreatedAt = DateTime.SpecifyKind(h.CreatedAt, DateTimeKind.Utc)
            }).ToList() : new List<OrderStatusHistoryDto>(),
            Items = o.OrderDetails.Select(od => new OrderDetailResponse
            {
                Id = od.Id,
                ProductId = od.ProductId,
                ProductName = od.ProductName,
                ItemType = od.ItemType,
                Size = od.Size,
                SugarLevel = od.SugarLevel,
                IceLevel = od.IceLevel,
                SpicyLevel = od.SpicyLevel,
                DippingSauce = od.DippingSauce,
                PortionSize = od.PortionSize,
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                TotalPrice = od.TotalPrice,
                IsCustomDIY = od.IsCustomDIY,
                CustomNotes = od.CustomNotes,
                ImageUrl = od.Product?.ImageUrl ?? string.Empty,
                Toppings = od.Options.Select(opt => new OrderItemOptionResponse
                {
                    ToppingId = opt.ToppingId ?? 0,
                    ToppingName = opt.ToppingName,
                    Price = opt.Price,
                    Quantity = opt.Quantity
                }).ToList()
            }).ToList()
        };
    }
}
