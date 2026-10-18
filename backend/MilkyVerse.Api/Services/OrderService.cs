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
    Task<ApiResponse<OrderResponse>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request);
    Task<ApiResponse<ValidatePromoResponse>> ValidatePromotionAsync(ValidatePromoRequest request);
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

            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                Subtotal = subtotal,
                DiscountAmount = discount,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                Status = "Pending",
                PaymentStatus = (request.PaymentMethod == "VIETQR" || request.PaymentMethod == "VNPAY") ? "Paid" : "Pending",
                PaymentMethod = request.PaymentMethod ?? "COD",
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                ShippingAddress = request.ShippingAddress.Trim(),
                Note = request.Note ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                OrderDetails = orderDetailsToCreate
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Record initial history
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Pending",
                Note = "Đơn hàng mới được tạo thành công.",
                CreatedAt = DateTime.UtcNow
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

    public async Task<ApiResponse<List<OrderResponse>>> GetOrdersAsync(int? userId = null)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.DeliveryAssignment)
                .ThenInclude(da => da!.Shipper)
            .AsQueryable();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToOrderResponse(o))
            .ToListAsync();

        return ApiResponse<List<OrderResponse>>.Ok(orders);
    }

    public async Task<ApiResponse<OrderResponse>> GetOrderByIdAsync(int orderId, int? userId = null)
    {
        var query = _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.DeliveryAssignment)
                .ThenInclude(da => da!.Shipper)
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

        return ApiResponse<OrderResponse>.Ok(MapToOrderResponse(order));
    }

    public async Task<ApiResponse<OrderResponse>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.DeliveryAssignment)
                .ThenInclude(da => da!.Shipper)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return ApiResponse<OrderResponse>.Fail("Không tìm thấy đơn hàng.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            order.Status = request.Status;
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
        {
            order.PaymentStatus = request.PaymentStatus;
        }

        order.UpdatedAt = DateTime.UtcNow;

        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = order.Status,
            Note = string.IsNullOrWhiteSpace(request.Note) ? $"Trạng thái đơn hàng cập nhật thành: {order.Status}" : request.Note,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return ApiResponse<OrderResponse>.Ok(MapToOrderResponse(order), "Cập nhật trạng thái đơn thành công!");
    }

    public async Task<ApiResponse<ValidatePromoResponse>> ValidatePromotionAsync(ValidatePromoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResponse<ValidatePromoResponse>.Fail("Vui lòng nhập mã ưu đãi.");
        }

        var code = request.Code.Trim().ToUpper();
        var now = DateTime.UtcNow;

        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => p.Code == code && p.IsActive);

        if (promo == null)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = code,
                DiscountAmount = 0,
                Message = "Mã ưu đãi không tồn tại hoặc đã hết hạn."
            });
        }

        if (now < promo.StartDate || now > promo.EndDate)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = code,
                DiscountAmount = 0,
                Message = "Mã ưu đãi đã hết thời gian áp dụng."
            });
        }

        if (promo.UsedCount >= promo.MaxUsage)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = code,
                DiscountAmount = 0,
                Message = "Mã ưu đãi đã hết lượt sử dụng."
            });
        }

        if (request.OrderSubtotal < promo.MinOrderValue)
        {
            return ApiResponse<ValidatePromoResponse>.Ok(new ValidatePromoResponse
            {
                IsValid = false,
                Code = code,
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
            Code = code,
            DiscountAmount = discount,
            Message = $"Áp dụng thành công mã {code}: Giảm {discount:N0}đ"
        });
    }

    private static OrderResponse MapToOrderResponse(Order o)
    {
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
            PaymentStatus = o.PaymentStatus,
            PaymentMethod = o.PaymentMethod,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            ShippingAddress = o.ShippingAddress,
            Note = o.Note,
            CreatedAt = o.CreatedAt,
            ShipperId = o.DeliveryAssignment != null ? o.DeliveryAssignment.ShipperId : null,
            ShipperName = o.DeliveryAssignment != null && o.DeliveryAssignment.Shipper != null ? o.DeliveryAssignment.Shipper.FullName : string.Empty,
            ShipperPhone = o.DeliveryAssignment != null && o.DeliveryAssignment.Shipper != null ? o.DeliveryAssignment.Shipper.PhoneNumber : string.Empty,
            DeliveryStatus = o.DeliveryAssignment != null ? o.DeliveryAssignment.Status : string.Empty,
            CodAmount = o.DeliveryAssignment != null ? o.DeliveryAssignment.CodAmount : (o.PaymentMethod == "COD" ? o.TotalAmount : 0),
            IsCodCollected = o.DeliveryAssignment != null && o.DeliveryAssignment.IsCodCollected,
            CodReconciled = o.DeliveryAssignment != null && o.DeliveryAssignment.CodReconciled,
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
