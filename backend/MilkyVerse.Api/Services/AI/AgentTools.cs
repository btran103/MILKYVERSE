using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services.AI;

/// <summary>
/// Context passed to each IAgentTool execution during a chat turn.
/// </summary>
public class AgentToolContext
{
    public int SessionId { get; set; }
    public int? UserId { get; set; }
    public string UserRole { get; set; } = "Guest";
    public List<SuggestedDrinkDto> SuggestedDrinks { get; set; } = new();
    public List<AIClientActionDto> Actions { get; set; } = new();
    public List<AIToolExecutionLogDto> ExecutedTools { get; set; } = new();

    public void AddSuggestedProduct(ProductDto p, string reason, string? sugar = null, string? ice = null, List<string>? toppings = null)
    {
        if (SuggestedDrinks.Any(s => s.ProductId == p.Id)) return;
        var isFood = string.Equals(p.ProductType, "FOOD", StringComparison.OrdinalIgnoreCase);
        SuggestedDrinks.Add(new SuggestedDrinkDto
        {
            ProductId = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ImageUrl = p.ImageUrl,
            Description = p.Description,
            EstimatedPrice = p.BasePrice,
            ProductType = p.ProductType,
            CategoryName = p.CategoryName,
            IsInStock = p.IsActive && p.StockStatus == 1,
            SugarRecommend = sugar ?? (isFood ? "Không áp dụng" : "50%"),
            IceRecommend = ice ?? (isFood ? "Không áp dụng" : "70%"),
            RecommendedToppings = toppings ?? (isFood
                ? new List<string> { !string.IsNullOrWhiteSpace(p.DefaultSauce) ? p.DefaultSauce : "Tương ớt" }
                : new List<string> { "Trân châu hoàng kim", "Kem cheese" }),
            Reason = reason
        });
    }
}

public class AgentToolOutput
{
    public bool Success { get; set; } = true;
    public string Summary { get; set; } = string.Empty;
    public object Data { get; set; } = new();

    public static AgentToolOutput Ok(object data, string summary) => new() { Success = true, Data = data, Summary = summary };
    public static AgentToolOutput Fail(string errorCode, string message) => new()
    {
        Success = false,
        Summary = message,
        Data = new { error = errorCode, message }
    };
}

/// <summary>
/// Standard interface for modular AI Agent Tools registered via Dependency Injection.
/// </summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    Dictionary<string, AIToolParameterSchema> Parameters { get; }
    List<string> RequiredParameters { get; }
    Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default);
}

internal static class AgentToolArgParser
{
    public static readonly HashSet<string> ValidSugarAndIceLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "0%", "30%", "50%", "70%", "100%"
    };

    public static readonly HashSet<string> ValidSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "M", "L", "Standard"
    };

    public static string? GetString(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => VietnameseNlpHelper.NormalizeNfc(el.GetString()),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    public static int? GetInt(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var sv)) return sv;
        return null;
    }

    public static decimal? GetDecimal(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var v)) return v;
        if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), out var sv)) return sv;
        return null;
    }

    public static bool? GetBool(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.True) return true;
        if (el.ValueKind == JsonValueKind.False) return false;
        if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var b)) return b;
        return null;
    }

    public static List<int> GetIntList(Dictionary<string, JsonElement> args, string key)
    {
        var list = new List<int>();
        if (!args.TryGetValue(key, out var el)) return list;
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var id))
                {
                    list.Add(id);
                }
                else if (item.ValueKind == JsonValueKind.String && int.TryParse(item.GetString(), out var sid))
                {
                    list.Add(sid);
                }
            }
        }
        else if (el.ValueKind == JsonValueKind.String)
        {
            var raw = el.GetString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(part, out var pid)) list.Add(pid);
                }
            }
        }
        return list;
    }
}

// ============================================================================
// 1. search_menu(query, maxPrice?, productType?, noCaffeine?, vegan?, maxSpicyLevel?, limit)
// ============================================================================
public class SearchMenuTool : IAgentTool
{
    private readonly IProductService _productService;

    public SearchMenuTool(IProductService productService)
    {
        _productService = productService;
    }

    public string Name => "search_menu";
    public string Description => "Tìm kiếm thực đơn MilkyVerse (chỉ các món đang kinh doanh IsActive). Hỗ trợ lọc theo từ khóa, ngân sách tối đa, loại món (DRINK/FOOD/COMBO), không caffeine, thuần chay (vegan), độ cay tối đa.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["query"] = new() { Type = "string", Description = "Từ khóa tên món hoặc hương vị (VD: trà sữa, matcha, đào, cá viên)" },
        ["minPrice"] = new() { Type = "number", Description = "Giá tối thiểu (VNĐ)" },
        ["maxPrice"] = new() { Type = "number", Description = "Giá tối đa / Ngân sách (VNĐ)" },
        ["productType"] = new() { Type = "string", Description = "DRINK, FOOD, hoặc COMBO", EnumValues = new() { "DRINK", "FOOD", "COMBO" } },
        ["noCaffeine"] = new() { Type = "boolean", Description = "True nếu khách cần đồ uống không chứa caffeine (buổi tối, sợ mất ngủ)" },
        ["vegan"] = new() { Type = "boolean", Description = "True nếu khách ăn chay / không dùng sữa động vật" },
        ["maxSpicyLevel"] = new() { Type = "integer", Description = "Cấp độ cay tối đa (0..7)" },
        ["limit"] = new() { Type = "integer", Description = "Số lượng món tối đa trả về (1..12, mặc định 6)" }
    };
    public List<string> RequiredParameters => new();

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        var query = AgentToolArgParser.GetString(args, "query");
        var minPrice = AgentToolArgParser.GetDecimal(args, "minPrice");
        var maxPrice = AgentToolArgParser.GetDecimal(args, "maxPrice");
        var productType = AgentToolArgParser.GetString(args, "productType")?.ToUpperInvariant();
        var noCaffeine = AgentToolArgParser.GetBool(args, "noCaffeine") ?? false;
        var vegan = AgentToolArgParser.GetBool(args, "vegan") ?? false;
        var maxSpicy = AgentToolArgParser.GetInt(args, "maxSpicyLevel");
        var limit = Math.Clamp(AgentToolArgParser.GetInt(args, "limit") ?? 6, 1, 12);

        if (maxPrice.HasValue && maxPrice.Value <= 0)
        {
            return AgentToolOutput.Fail("INVALID_MAX_PRICE", "Ngân sách tối đa (maxPrice) phải lớn hơn 0.");
        }

        // Reuse IProductService.GetProductsAsync (which already filters p.IsActive)
        var res = await _productService.GetProductsAsync(
            categoryId: null,
            search: null,
            minPrice: minPrice,
            maxPrice: maxPrice,
            sortBy: null,
            productType: productType);

        var items = (res.Data ?? new List<ProductDto>())
            .Where(p => p.IsActive && p.StockStatus == 1)
            .ToList();

        if (noCaffeine)
        {
            items = items.Where(p =>
                p.Slug.Contains("nuoc-ep") ||
                p.Slug.Contains("soda") ||
                p.Slug.Contains("sua-tuoi") ||
                p.Slug.Contains("sua-chua")).ToList();
        }

        if (vegan)
        {
            items = items.Where(p =>
                p.Slug.Contains("yen-mach") ||
                p.Slug.Contains("oat") ||
                p.Slug.Contains("nuoc-ep") ||
                p.Slug.Contains("soda") ||
                (p.ProductType == "DRINK" && !p.Slug.Contains("tra-sua") && !p.Slug.Contains("kem-cheese") && !p.Slug.Contains("sua-tuoi"))).ToList();
        }

        if (maxSpicy.HasValue)
        {
            items = items.Where(p => (p.SpicyLevel ?? 0) <= maxSpicy.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var intent = VietnameseNlpHelper.AnalyzeMenuQuery(query);
            var qAscii = VietnameseNlpHelper.RemoveDiacritics(query);
            var cleanedQ = VietnameseNlpHelper.CleanKeywordsFromText(qAscii);
            var tokens = cleanedQ.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tokens.Count == 0)
            {
                tokens = qAscii.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length >= 2).ToList();
            }

            var scored = items
                .Select(p =>
                {
                    var nameAscii = VietnameseNlpHelper.RemoveDiacritics(p.Name);
                    var catAscii = VietnameseNlpHelper.RemoveDiacritics(p.CategoryName);
                    var descAscii = VietnameseNlpHelper.RemoveDiacritics(p.Description);
                    int score = 0;

                    foreach (var grp in intent.OrGroups)
                    {
                        if (grp.Length < 3) continue;
                        if (nameAscii.Contains(grp)) score += 20;
                        else if (catAscii.Contains(grp)) score += 12;
                        else if (descAscii.Contains(grp)) score += 6;
                    }

                    if (nameAscii.Contains(qAscii)) score += 15;
                    if (catAscii.Contains(qAscii)) score += 10;
                    if (p.Slug.ToLowerInvariant().Contains(qAscii.Replace(" ", "-"))) score += 10;
                    if (descAscii.Contains(qAscii)) score += 4;
                    foreach (var tk in tokens)
                    {
                        if (nameAscii.Contains(tk)) score += 4;
                        if (catAscii.Contains(tk)) score += 3;
                        if (descAscii.Contains(tk)) score += 1;
                    }
                    return (Product: p, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Product.IsBestSeller)
                .Select(x => x.Product)
                .ToList();

            items = scored;
        }

        var selected = items.Take(limit).ToList();
        string sugarRec = vegan ? "30%" : "50%";
        string iceRec = "70%";
        string reason = maxPrice.HasValue
            ? $"Phù hợp ngân sách ≤ {maxPrice.Value:N0}đ."
            : noCaffeine
                ? "Đồ uống 0% Caffeine không lo mất ngủ."
                : "Món ngon đang kinh doanh tại MilkyVerse.";

        foreach (var p in selected)
        {
            context.AddSuggestedProduct(p, reason, sugarRec, iceRec);
        }

        return AgentToolOutput.Ok(new
        {
            totalMatched = items.Count,
            products = selected.Select(p => new
            {
                productId = p.Id,
                name = p.Name,
                slug = p.Slug,
                categoryName = p.CategoryName,
                productType = p.ProductType,
                basePrice = p.BasePrice,
                imageUrl = p.ImageUrl,
                description = p.Description,
                spicyLevel = p.SpicyLevel
            })
        }, $"Tìm thấy {selected.Count} món phù hợp.");
    }
}

// ============================================================================
// 2. get_product_detail(productId)
// ============================================================================
public class GetProductDetailTool : IAgentTool
{
    private readonly IProductService _productService;

    public GetProductDetailTool(IProductService productService)
    {
        _productService = productService;
    }

    public string Name => "get_product_detail";
    public string Description => "Lấy thông tin chi tiết của một sản phẩm theo productId (giá gốc, mô tả, kích cỡ, độ ngọt, mức đá, thành phần, đánh giá).";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["productId"] = new() { Type = "integer", Description = "ID sản phẩm trong database" }
    };
    public List<string> RequiredParameters => new() { "productId" };

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        var productId = AgentToolArgParser.GetInt(args, "productId");
        if (!productId.HasValue || productId.Value <= 0)
        {
            return AgentToolOutput.Fail("INVALID_PRODUCT_ID", "Tham số productId không hợp lệ.");
        }

        var res = await _productService.GetProductByIdAsync(productId.Value);
        if (!res.Success || res.Data == null || !res.Data.IsActive)
        {
            return AgentToolOutput.Fail("PRODUCT_NOT_FOUND", $"Không tìm thấy sản phẩm có ID = {productId.Value} hoặc sản phẩm đã ngừng kinh doanh.");
        }

        var p = res.Data;
        context.AddSuggestedProduct(p, $"Thông tin chi tiết món {p.Name}.");

        return AgentToolOutput.Ok(new
        {
            productId = p.Id,
            name = p.Name,
            slug = p.Slug,
            categoryName = p.CategoryName,
            productType = p.ProductType,
            basePrice = p.BasePrice,
            priceSizeL = p.ProductType == "DRINK" ? p.BasePrice + 8000 : p.BasePrice,
            imageUrl = p.ImageUrl,
            description = p.Description,
            sizes = p.Sizes,
            sugarLevels = p.SugarLevels,
            iceLevels = p.IceLevels,
            ingredients = p.Ingredients,
            averageRating = p.AverageRating
        }, $"Chi tiết món '{p.Name}' ({p.BasePrice:N0}đ).");
    }
}

// ============================================================================
// 3. list_toppings()
// ============================================================================
public class ListToppingsTool : IAgentTool
{
    private readonly IProductService _productService;

    public ListToppingsTool(IProductService productService)
    {
        _productService = productService;
    }

    public string Name => "list_toppings";
    public string Description => "Lấy danh sách các loại topping hiện đang phục vụ tại MilkyVerse kèm ID và giá tiền.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new();
    public List<string> RequiredParameters => new();

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        var res = await _productService.GetToppingsAsync();
        var toppings = res.Data ?? new List<ToppingDto>();

        return AgentToolOutput.Ok(new
        {
            count = toppings.Count,
            toppings = toppings.Select(t => new
            {
                toppingId = t.Id,
                name = t.Name,
                price = t.Price
            })
        }, $"Hiện có {toppings.Count} loại topping khả dụng.");
    }
}

// ============================================================================
// 4. quote_diy_drink(teaBaseId, milkId, size, sugar, ice, toppingIds[])
// ============================================================================
public class QuoteDiyDrinkTool : IAgentTool
{
    private readonly IDIYService _diyService;

    public QuoteDiyDrinkTool(IDIYService diyService)
    {
        _diyService = diyService;
    }

    public string Name => "quote_diy_drink";
    public string Description => "Tính giá và kiểm tra cấu hình ly trà sữa tự pha DIY Drink Studio (gọi DIYService.QuoteDrinkAsync). Trả về giá chi tiết và client action OPEN_DIY.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["teaBaseId"] = new() { Type = "string", Description = "Mã cốt trà: black_tea, green_tea, oolong_tea, matcha_base", EnumValues = new() { "black_tea", "green_tea", "oolong_tea", "matcha_base" } },
        ["milkId"] = new() { Type = "string", Description = "Mã loại sữa: fresh_milk, condensed_milk, oat_milk, almond_milk", EnumValues = new() { "fresh_milk", "condensed_milk", "oat_milk", "almond_milk" } },
        ["size"] = new() { Type = "string", Description = "Kích cỡ: M hoặc L", EnumValues = new() { "M", "L" } },
        ["sugar"] = new() { Type = "string", Description = "Mức đường: 0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
        ["ice"] = new() { Type = "string", Description = "Mức đá: 0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
        ["toppingIds"] = new() { Type = "array", ItemsType = "integer", Description = "Danh sách ID các topping muốn phối kèm" }
    };
    public List<string> RequiredParameters => new() { "teaBaseId", "milkId", "size", "sugar", "ice" };

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        var teaBaseId = AgentToolArgParser.GetString(args, "teaBaseId")?.Trim() ?? "";
        var milkId = AgentToolArgParser.GetString(args, "milkId")?.Trim() ?? "";
        var size = AgentToolArgParser.GetString(args, "size")?.Trim().ToUpperInvariant() ?? "M";
        var sugar = AgentToolArgParser.GetString(args, "sugar")?.Trim() ?? "50%";
        var ice = AgentToolArgParser.GetString(args, "ice")?.Trim() ?? "70%";
        var toppingIds = AgentToolArgParser.GetIntList(args, "toppingIds");

        if (!AgentToolArgParser.ValidSugarAndIceLevels.Contains(sugar))
        {
            return AgentToolOutput.Fail("INVALID_SUGAR_LEVEL", $"Mức đường '{sugar}' không hợp lệ. Chỉ chấp nhận: 0%, 30%, 50%, 70%, 100%.");
        }

        if (!AgentToolArgParser.ValidSugarAndIceLevels.Contains(ice))
        {
            return AgentToolOutput.Fail("INVALID_ICE_LEVEL", $"Mức đá '{ice}' không hợp lệ. Chỉ chấp nhận: 0%, 30%, 50%, 70%, 100%.");
        }

        var quoteRes = await _diyService.QuoteDrinkAsync(new DIYQuoteRequest
        {
            TeaBaseId = teaBaseId,
            MilkId = milkId,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            ToppingIds = toppingIds,
            Quantity = 1
        });

        if (!quoteRes.Success || quoteRes.Data == null)
        {
            return AgentToolOutput.Fail("INVALID_DIY_QUOTE", quoteRes.Message);
        }

        var q = quoteRes.Data;
        var diyUrl = $"/diy?tea={teaBaseId}&milk={milkId}&size={size}&sugar={Uri.EscapeDataString(sugar)}&ice={Uri.EscapeDataString(ice)}";

        context.Actions.Add(new AIClientActionDto
        {
            Type = "OPEN_DIY",
            Payload = new
            {
                recipeName = q.RecipeName,
                teaBaseId,
                milkId,
                size,
                sugarLevel = sugar,
                iceLevel = ice,
                toppingIds,
                unitPrice = q.UnitPrice,
                breakdown = q.Breakdown,
                diyUrl
            }
        });

        return AgentToolOutput.Ok(new
        {
            recipeName = q.RecipeName,
            unitPrice = q.UnitPrice,
            totalPrice = q.TotalPrice,
            breakdown = q.Breakdown,
            diyUrl
        }, $"Báo giá DIY '{q.RecipeName}': {q.UnitPrice:N0}đ.");
    }
}

// ============================================================================
// 5. validate_coupon(code, subtotal)
// ============================================================================
public class ValidateCouponTool : IAgentTool
{
    private readonly IOrderService _orderService;

    public ValidateCouponTool(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public string Name => "validate_coupon";
    public string Description => "Kiểm tra mã giảm giá (voucher/coupon) dựa trên tạm tính đơn hàng (subtotal). Nếu hợp lệ sẽ trả về client action APPLY_COUPON.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["code"] = new() { Type = "string", Description = "Mã giảm giá cần kiểm tra (VD: MILKYNEW, FREESHIP, AIBARISTA)" },
        ["subtotal"] = new() { Type = "number", Description = "Tổng tiền tạm tính hiện tại của giỏ hàng (VNĐ)" }
    };
    public List<string> RequiredParameters => new() { "code", "subtotal" };

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        var code = AgentToolArgParser.GetString(args, "code")?.Trim().ToUpperInvariant() ?? "";
        var subtotal = AgentToolArgParser.GetDecimal(args, "subtotal") ?? 0;

        if (string.IsNullOrWhiteSpace(code))
        {
            return AgentToolOutput.Fail("INVALID_COUPON_CODE", "Vui lòng nhập mã giảm giá cần kiểm tra.");
        }

        if (subtotal < 0)
        {
            return AgentToolOutput.Fail("INVALID_SUBTOTAL", "Giá trị tạm tính (subtotal) không được âm.");
        }

        var res = await _orderService.ValidatePromotionAsync(new ValidatePromoRequest
        {
            Code = code,
            OrderSubtotal = subtotal
        });

        var promoData = res.Data;
        if (!res.Success || promoData == null || !promoData.IsValid)
        {
            return AgentToolOutput.Fail("COUPON_INVALID", promoData?.Message ?? res.Message ?? "Mã giảm giá không hợp lệ hoặc chưa đủ điều kiện.");
        }

        context.Actions.Add(new AIClientActionDto
        {
            Type = "APPLY_COUPON",
            Payload = new
            {
                code = promoData.Code,
                discountAmount = promoData.DiscountAmount,
                subtotal,
                message = promoData.Message
            }
        });

        return AgentToolOutput.Ok(new
        {
            isValid = true,
            code = promoData.Code,
            discountAmount = promoData.DiscountAmount,
            message = promoData.Message
        }, $"Mã '{promoData.Code}' hợp lệ: Giảm {promoData.DiscountAmount:N0}đ.");
    }
}

// ============================================================================
// 6. get_my_orders(limit)
// ============================================================================
public class GetMyOrdersTool : IAgentTool
{
    private readonly IOrderService _orderService;

    public GetMyOrdersTool(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public string Name => "get_my_orders";
    public string Description => "Lấy danh sách các đơn hàng của chính người dùng đang đăng nhập.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["limit"] = new() { Type = "integer", Description = "Số lượng đơn tối đa cần lấy (1..15, mặc định 5)" }
    };
    public List<string> RequiredParameters => new();

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        if (!context.UserId.HasValue || context.UserId.Value <= 0)
        {
            return AgentToolOutput.Fail("UNAUTHORIZED", "Bạn cần đăng nhập tài khoản để xem lịch sử đơn hàng của mình.");
        }

        var limit = Math.Clamp(AgentToolArgParser.GetInt(args, "limit") ?? 5, 1, 15);
        var res = await _orderService.GetOrdersAsync(context.UserId.Value);
        var orders = (res.Data ?? new List<OrderResponse>())
            .Where(o => o.UserId == context.UserId.Value)
            .Take(limit)
            .ToList();

        return AgentToolOutput.Ok(new
        {
            count = orders.Count,
            orders = orders.Select(o => new
            {
                orderId = o.Id,
                orderCode = o.OrderCode,
                status = o.Status,
                paymentStatus = o.PaymentStatus,
                paymentMethod = o.PaymentMethod,
                totalAmount = o.TotalAmount,
                createdAt = o.CreatedAt,
                orderUrl = $"/orders/{o.Id}",
                items = o.Items.Select(i => $"{i.Quantity}x {i.ProductName} (Size {i.Size})")
            })
        }, $"Tìm thấy {orders.Count} đơn hàng của bạn.");
    }
}

// ============================================================================
// 7. get_order_status(orderCode)
// ============================================================================
public class GetOrderStatusTool : IAgentTool
{
    private readonly IOrderService _orderService;

    public GetOrderStatusTool(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public string Name => "get_order_status";
    public string Description => "Tra cứu trạng thái chi tiết của một đơn hàng theo mã đơn (orderCode). Chỉ cho phép khi đã đăng nhập và đơn thuộc về chính người dùng đó.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["orderCode"] = new() { Type = "string", Description = "Mã đơn hàng (VD: MV2026...)" }
    };
    public List<string> RequiredParameters => new() { "orderCode" };

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        if (!context.UserId.HasValue || context.UserId.Value <= 0)
        {
            return AgentToolOutput.Fail("UNAUTHORIZED", "Bạn cần đăng nhập tài khoản để tra cứu trạng thái đơn hàng.");
        }

        var orderCode = AgentToolArgParser.GetString(args, "orderCode")?.Trim().Replace("#", "") ?? "";
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            return AgentToolOutput.Fail("INVALID_ORDER_CODE", "Vui lòng cung cấp mã đơn hàng (orderCode).");
        }

        // Only query orders owned by context.UserId.Value
        var userOrdersRes = await _orderService.GetOrdersAsync(context.UserId.Value);
        var matched = (userOrdersRes.Data ?? new List<OrderResponse>())
            .FirstOrDefault(o =>
                o.UserId == context.UserId.Value &&
                string.Equals(o.OrderCode, orderCode, StringComparison.OrdinalIgnoreCase));

        if (matched == null)
        {
            return AgentToolOutput.Fail("ORDER_NOT_FOUND_OR_FORBIDDEN", $"Không tìm thấy đơn hàng '{orderCode}' thuộc tài khoản của bạn.");
        }

        return AgentToolOutput.Ok(new
        {
            orderId = matched.Id,
            orderCode = matched.OrderCode,
            status = matched.Status,
            paymentStatus = matched.PaymentStatus,
            paymentMethod = matched.PaymentMethod,
            totalAmount = matched.TotalAmount,
            createdAt = matched.CreatedAt,
            orderUrl = $"/orders/{matched.Id}",
            items = matched.Items.Select(i => new
            {
                i.ProductName,
                i.Size,
                i.SugarLevel,
                i.IceLevel,
                i.Quantity,
                i.TotalPrice
            })
        }, $"Đơn #{matched.OrderCode}: Trạng thái '{matched.Status}', Thanh toán '{matched.PaymentStatus}', Tổng {matched.TotalAmount:N0}đ.");
    }
}

// ============================================================================
// 8. add_to_cart(productId, size?, sugar?, ice?, toppingIds?, quantity?)
//    Validates all parameters on server; does NOT write cart to DB; emits ADD_TO_CART client action!
// ============================================================================
public class AddToCartTool : IAgentTool
{
    private readonly MilkyVerseDbContext _db;

    public AddToCartTool(MilkyVerseDbContext db)
    {
        _db = db;
    }

    public string Name => "add_to_cart";
    public string Description => "Kiểm tra tính hợp lệ của món, kích cỡ, đường, đá, topping, số lượng (1..20), tính giá thực tế từ DB và trả về client action ADD_TO_CART cho Frontend thêm vào giỏ hàng.";
    public Dictionary<string, AIToolParameterSchema> Parameters => new(StringComparer.OrdinalIgnoreCase)
    {
        ["productId"] = new() { Type = "integer", Description = "ID sản phẩm có thật trong database" },
        ["quantity"] = new() { Type = "integer", Description = "Số lượng cần thêm (bắt buộc từ 1 đến 20)" },
        ["size"] = new() { Type = "string", Description = "Kích cỡ: M, L, hoặc Standard", EnumValues = new() { "M", "L", "Standard" } },
        ["sugar"] = new() { Type = "string", Description = "Mức đường: 0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
        ["ice"] = new() { Type = "string", Description = "Mức đá: 0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
        ["toppingIds"] = new() { Type = "array", ItemsType = "integer", Description = "Danh sách ID topping thêm kèm" }
    };
    public List<string> RequiredParameters => new() { "productId" };

    public async Task<AgentToolOutput> ExecuteAsync(Dictionary<string, JsonElement> args, AgentToolContext context, CancellationToken ct = default)
    {
        var productId = AgentToolArgParser.GetInt(args, "productId");
        if (!productId.HasValue || productId.Value <= 0)
        {
            return AgentToolOutput.Fail("INVALID_PRODUCT_ID", "productId không hợp lệ.");
        }

        var quantity = AgentToolArgParser.GetInt(args, "quantity") ?? 1;
        if (quantity < 1 || quantity > 20)
        {
            return AgentToolOutput.Fail("INVALID_QUANTITY", $"Số lượng ({quantity}) không hợp lệ. Chỉ cho phép đặt từ 1 đến 20 phần mỗi lần.");
        }

        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId.Value && p.IsActive, ct);

        if (product == null)
        {
            return AgentToolOutput.Fail("PRODUCT_NOT_FOUND", $"Sản phẩm ID={productId.Value} không tồn tại hoặc đã ngừng kinh doanh.");
        }

        if (product.StockStatus == 0)
        {
            return AgentToolOutput.Fail("OUT_OF_STOCK", $"Món '{product.Name}' hiện đang tạm hết hàng.");
        }

        var isFood = string.Equals(product.ProductType, "FOOD", StringComparison.OrdinalIgnoreCase);
        var rawSize = AgentToolArgParser.GetString(args, "size")?.Trim() ?? (isFood ? "Standard" : "M");
        if (!AgentToolArgParser.ValidSizes.Contains(rawSize))
        {
            return AgentToolOutput.Fail("INVALID_SIZE", $"Kích cỡ '{rawSize}' không hợp lệ. Chỉ chấp nhận M, L hoặc Standard.");
        }

        var effectiveSize = isFood ? "Standard" : rawSize.ToUpperInvariant();
        var sugar = AgentToolArgParser.GetString(args, "sugar")?.Trim() ?? "50%";
        var ice = AgentToolArgParser.GetString(args, "ice")?.Trim() ?? "70%";

        if (!isFood)
        {
            if (!AgentToolArgParser.ValidSugarAndIceLevels.Contains(sugar))
            {
                return AgentToolOutput.Fail("INVALID_SUGAR_LEVEL", $"Mức đường '{sugar}' không hợp lệ. Chỉ chấp nhận: 0%, 30%, 50%, 70%, 100%.");
            }
            if (!AgentToolArgParser.ValidSugarAndIceLevels.Contains(ice))
            {
                return AgentToolOutput.Fail("INVALID_ICE_LEVEL", $"Mức đá '{ice}' không hợp lệ. Chỉ chấp nhận: 0%, 30%, 50%, 70%, 100%.");
            }
        }

        var toppingIds = AgentToolArgParser.GetIntList(args, "toppingIds");
        var resolvedToppings = new List<Topping>();
        if (toppingIds.Any())
        {
            resolvedToppings = await _db.Toppings
                .Where(t => toppingIds.Contains(t.Id) && t.IsAvailable)
                .ToListAsync(ct);

            if (resolvedToppings.Count != toppingIds.Distinct().Count())
            {
                return AgentToolOutput.Fail("INVALID_TOPPING", "Một hoặc nhiều topping được chọn không tồn tại hoặc đã hết.");
            }
        }

        decimal sizeSurcharge = (!isFood && effectiveSize == "L") ? 8000 : 0;
        decimal toppingsPrice = resolvedToppings.Sum(t => t.Price);
        decimal unitPrice = product.BasePrice + sizeSurcharge + toppingsPrice;
        decimal totalPrice = unitPrice * quantity;

        var actionPayload = new
        {
            productId = product.Id,
            name = product.Name,
            slug = product.Slug,
            imageUrl = product.ImageUrl,
            itemType = product.ProductType ?? "DRINK",
            size = effectiveSize,
            sugarLevel = isFood ? "" : sugar,
            iceLevel = isFood ? "" : ice,
            unitPrice,
            quantity,
            totalPrice,
            isCustomDIY = false,
            customNotes = isFood ? "" : $"AI Barista ({sugar} đường, {ice} đá)",
            toppings = resolvedToppings.Select(t => new
            {
                toppingId = t.Id,
                name = t.Name,
                price = t.Price,
                quantity = 1
            }).ToList()
        };

        context.Actions.Add(new AIClientActionDto
        {
            Type = "ADD_TO_CART",
            Payload = actionPayload
        });

        context.AddSuggestedProduct(new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "",
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            StockStatus = product.StockStatus,
            ProductType = product.ProductType ?? "DRINK"
        }, $"Đã chuẩn bị thêm {quantity}x {product.Name} vào giỏ hàng.", sugar, ice, resolvedToppings.Select(t => t.Name).ToList());

        return AgentToolOutput.Ok(actionPayload, $"Đã tạo lệnh thêm {quantity}x '{product.Name}' (Size {effectiveSize}, {unitPrice:N0}đ) vào giỏ hàng.");
    }
}
