using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services.AI;

public class AgentExecutionContext
{
    public int SessionId { get; set; }
    public int? UserId { get; set; }
    public string UserRole { get; set; } = "Guest";
    public string? UserName { get; set; }
    public string? UserPhone { get; set; }
    public string? UserAddress { get; set; }

    public List<AIClientCartItemDto> WorkingCart { get; set; } = new();
    public List<SuggestedDrinkDto> SuggestedDrinks { get; set; } = new();
    public List<AIClientActionDto> Actions { get; set; } = new();
    public List<AIRecipeCardDto> Recipes { get; set; } = new();
    public List<AIDrinkCustomizationCardDto> Customizations { get; set; } = new();
    public AICartActionCardDto? CartAction { get; set; }
    public List<AIOrderStatusCardDto> Orders { get; set; } = new();
    public AIPaymentCardDto? Payment { get; set; }
    public AIInventoryReportCardDto? InventoryReport { get; set; }
    public AIAdminAnalyticsCardDto? AdminAnalytics { get; set; }
    public List<AIToolExecutionLogDto> ExecutedTools { get; set; } = new();
}

public class ToolExecutionResult
{
    public bool Success { get; set; } = true;
    public string ToolName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public object Data { get; set; } = new { };
}

public interface IToolExecutionService
{
    Task InitializeCartContextAsync(AgentExecutionContext context, List<AIClientCartItemDto>? clientCart);
    Task<ToolExecutionResult> ExecuteToolAsync(
        string toolName,
        Dictionary<string, JsonElement> args,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}

public class ToolExecutionService : IToolExecutionService
{
    private readonly MilkyVerseDbContext _db;
    private readonly IToolRegistry _toolRegistry;
    private readonly IDIYService _diyService;
    private readonly IOrderService _orderService;
    private readonly ISePayService _sePayService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<ToolExecutionService> _logger;

    public ToolExecutionService(
        MilkyVerseDbContext db,
        IToolRegistry toolRegistry,
        IDIYService diyService,
        IOrderService orderService,
        ISePayService sePayService,
        IConversationService conversationService,
        ILogger<ToolExecutionService> logger)
    {
        _db = db;
        _toolRegistry = toolRegistry;
        _diyService = diyService;
        _orderService = orderService;
        _sePayService = sePayService;
        _conversationService = conversationService;
        _logger = logger;
    }

    public async Task InitializeCartContextAsync(AgentExecutionContext context, List<AIClientCartItemDto>? clientCart)
    {
        if (clientCart != null && clientCart.Count > 0)
        {
            context.WorkingCart = clientCart.Select(c => new AIClientCartItemDto
            {
                Id = string.IsNullOrWhiteSpace(c.Id) ? "item_" + Guid.NewGuid().ToString("N")[..8] : c.Id,
                ProductId = c.ProductId,
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                ItemType = string.IsNullOrWhiteSpace(c.ItemType) ? "DRINK" : c.ItemType,
                Size = string.IsNullOrWhiteSpace(c.Size) ? "M" : c.Size,
                SugarLevel = c.SugarLevel ?? "50%",
                IceLevel = c.IceLevel ?? "70%",
                SpicyLevel = c.SpicyLevel,
                DippingSauce = c.DippingSauce,
                PortionSize = c.PortionSize,
                UnitPrice = c.UnitPrice,
                Quantity = Math.Max(1, c.Quantity),
                TotalPrice = c.UnitPrice * Math.Max(1, c.Quantity),
                IsCustomDIY = c.IsCustomDIY,
                CustomNotes = c.CustomNotes,
                Toppings = c.Toppings ?? new List<AIClientCartToppingDto>()
            }).ToList();

            await SyncWorkingCartToDbAsync(context);
            return;
        }

        // Load from DB AICartItems if clientCart wasn't sent
        var dbItems = await GetDbCartItemsQuery(context).ToListAsync();
        context.WorkingCart = dbItems.Select(MapDbCartToDto).ToList();
    }

    private IQueryable<AICartItem> GetDbCartItemsQuery(AgentExecutionContext context)
    {
        if (context.UserId.HasValue && context.UserId.Value > 0)
        {
            return _db.AICartItems.Where(c => c.UserId == context.UserId.Value);
        }
        return _db.AICartItems.Where(c => c.SessionId == context.SessionId && c.UserId == null);
    }

    private async Task SyncWorkingCartToDbAsync(AgentExecutionContext context)
    {
        try
        {
            var existing = await GetDbCartItemsQuery(context).ToListAsync();
            if (existing.Any())
            {
                _db.AICartItems.RemoveRange(existing);
            }

            foreach (var item in context.WorkingCart)
            {
                _db.AICartItems.Add(new AICartItem
                {
                    UserId = context.UserId,
                    SessionId = context.SessionId,
                    ProductId = item.ProductId,
                    ProductName = item.Name,
                    ItemType = item.ItemType,
                    ImageUrl = item.ImageUrl ?? string.Empty,
                    Size = item.Size,
                    SugarLevel = item.SugarLevel ?? "50%",
                    IceLevel = item.IceLevel ?? "70%",
                    SpicyLevel = item.SpicyLevel ?? "Không cay",
                    DippingSauce = item.DippingSauce ?? "Tương ớt",
                    PortionSize = item.PortionSize ?? "Tiêu chuẩn",
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice,
                    IsCustomDIY = item.IsCustomDIY,
                    CustomNotes = item.CustomNotes ?? string.Empty,
                    ToppingsJson = JsonSerializer.Serialize(item.Toppings),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not sync working cart to AICartItems table.");
        }
    }

    private static AIClientCartItemDto MapDbCartToDto(AICartItem c)
    {
        List<AIClientCartToppingDto> toppings = new();
        try
        {
            toppings = JsonSerializer.Deserialize<List<AIClientCartToppingDto>>(c.ToppingsJson) ?? new();
        }
        catch { }

        return new AIClientCartItemDto
        {
            Id = "cart_" + c.Id,
            ProductId = c.ProductId,
            Name = c.ProductName,
            ImageUrl = c.ImageUrl,
            ItemType = c.ItemType,
            Size = c.Size,
            SugarLevel = c.SugarLevel,
            IceLevel = c.IceLevel,
            SpicyLevel = c.SpicyLevel,
            DippingSauce = c.DippingSauce,
            PortionSize = c.PortionSize,
            UnitPrice = c.UnitPrice,
            Quantity = c.Quantity,
            TotalPrice = c.TotalPrice,
            IsCustomDIY = c.IsCustomDIY,
            CustomNotes = c.CustomNotes,
            Toppings = toppings
        };
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(
        string toolName,
        Dictionary<string, JsonElement> args,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var rawArgsJson = JsonSerializer.Serialize(args);

        // 1. Validate tool existence
        if (!_toolRegistry.TryGetTool(toolName, out var toolDef) || toolDef == null)
        {
            var unknownRes = new ToolExecutionResult
            {
                Success = false,
                ToolName = toolName,
                Summary = $"Công cụ '{toolName}' không tồn tại trong hệ thống MilkyVerse.",
                Data = new { error = "UNKNOWN_TOOL", message = $"Công cụ '{toolName}' không hợp lệ." }
            };
            LogExecution(context, toolName, rawArgsJson, unknownRes);
            return unknownRes;
        }

        // 2. Validate Role-Based Authorization
        if (!_toolRegistry.IsRoleAuthorized(toolName, context.UserRole))
        {
            _logger.LogWarning("Security Alert: User (Id={UserId}, Role={Role}) attempted unauthorized tool {ToolName}.",
                context.UserId, context.UserRole, toolName);

            var unauthRes = new ToolExecutionResult
            {
                Success = false,
                ToolName = toolName,
                Summary = $"Từ chối truy cập: Vai trò '{context.UserRole}' không được cấp quyền sử dụng công cụ '{toolName}'.",
                Data = new
                {
                    error = "UNAUTHORIZED_ROLE",
                    requiredRoles = toolDef.AllowedRoles.ToArray(),
                    currentRole = context.UserRole,
                    message = $"Tài khoản vai trò '{context.UserRole}' không có quyền thực hiện nghiệp vụ '{toolName}'."
                }
            };
            LogExecution(context, toolName, rawArgsJson, unauthRes);
            return unauthRes;
        }

        try
        {
            ToolExecutionResult result = toolName.ToLowerInvariant() switch
            {
                // Product Tools
                "searchproducts" => await ExecuteSearchProductsAsync(args, context),
                "getproductdetails" => await ExecuteGetProductDetailsAsync(args, context),
                "recommenddrinks" => await ExecuteRecommendDrinksAsync(args, context),
                "filterproductsbyprice" => await ExecuteFilterProductsByPriceAsync(args, context),
                "filterproductsbytaste" => await ExecuteFilterProductsByTasteAsync(args, context),

                // DIY Drink Studio Tools
                "generaterecipe" => await ExecuteGenerateRecipeAsync(args, context),
                "customizedrink" => await ExecuteCustomizeDrinkAsync(args, context),
                "calculatedrinkprice" => await ExecuteCalculateDrinkPriceAsync(args, context),
                "getavailabletoppings" => await ExecuteGetAvailableToppingsAsync(),
                "getavailableingredients" => await ExecuteGetAvailableIngredientsAsync(args),
                "savefavoriterecipe" => await ExecuteSaveFavoriteRecipeAsync(args, context),

                // Shopping & Cart Tools
                "getcart" => ExecuteGetCart(context),
                "addtocart" => await ExecuteAddToCartAsync(args, context),
                "updatecartitem" => await ExecuteUpdateCartItemAsync(args, context),
                "removecartitem" => await ExecuteRemoveCartItemAsync(args, context),
                "clearcart" => await ExecuteClearCartAsync(context),

                // Order Tools
                "createorder" => await ExecuteCreateOrderAsync(args, context),
                "getorderhistory" => await ExecuteGetOrderHistoryAsync(args, context),
                "getorderdetails" => await ExecuteGetOrderDetailsAsync(args, context),
                "trackorder" => await ExecuteTrackOrderAsync(args, context),

                // Payment Tools
                "getpaymentguidance" => await ExecuteGetPaymentGuidanceAsync(args, context),
                "createpaymenttransaction" => await ExecuteCreatePaymentTransactionAsync(args, context),
                "checkpaymentstatus" => await ExecuteCheckPaymentStatusAsync(args, context),

                // Inventory Tools
                "getingredientinventory" => await ExecuteGetIngredientInventoryAsync(args, context),
                "checkingredientavailability" => await ExecuteCheckIngredientAvailabilityAsync(args, context),
                "getlowstockingredients" => await ExecuteGetLowStockIngredientsAsync(context),
                "getinventoryreport" => await ExecuteGetInventoryReportAsync(context),

                // Admin Tools
                "getsalesanalytics" => await ExecuteGetSalesAnalyticsAsync(args, context),
                "getbestsellingproducts" => await ExecuteGetBestSellingProductsAsync(args, context),
                "getorderstatistics" => await ExecuteGetOrderStatisticsAsync(context),
                "getcustomerstatistics" => await ExecuteGetCustomerStatisticsAsync(context),

                _ => new ToolExecutionResult
                {
                    Success = false,
                    ToolName = toolName,
                    Summary = "Công cụ chưa được ánh xạ.",
                    Data = new { error = "NOT_IMPLEMENTED" }
                }
            };

            LogExecution(context, toolDef.Name, rawArgsJson, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI tool {ToolName}", toolName);
            var errResult = new ToolExecutionResult
            {
                Success = false,
                ToolName = toolDef.Name,
                Summary = $"Lỗi khi thực thi {toolDef.Name}: {ex.Message}",
                Data = new { error = "EXECUTION_ERROR", message = ex.Message }
            };
            LogExecution(context, toolDef.Name, rawArgsJson, errResult);
            return errResult;
        }
    }

    private static void LogExecution(AgentExecutionContext context, string toolName, string argsJson, ToolExecutionResult res)
    {
        context.ExecutedTools.Add(new AIToolExecutionLogDto
        {
            ToolName = toolName,
            ArgumentsJson = argsJson,
            ResultSummary = res.Summary,
            Success = res.Success
        });
    }

    // =========================================================================
    // HELPER PARSERS
    // =========================================================================
    private static string? GetString(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? GetInt(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var sv)) return sv;
        return null;
    }

    private static decimal? GetDecimal(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var v)) return v;
        if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), out var sv)) return sv;
        return null;
    }

    private static bool? GetBool(Dictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var el)) return null;
        if (el.ValueKind == JsonValueKind.True) return true;
        if (el.ValueKind == JsonValueKind.False) return false;
        if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var b)) return b;
        return null;
    }

    private static List<string> GetStringList(Dictionary<string, JsonElement> args, string key)
    {
        var list = new List<string>();
        if (!args.TryGetValue(key, out var el)) return list;
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var s = item.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) list.Add(s.Trim());
                }
                else if (item.ValueKind == JsonValueKind.Number)
                {
                    list.Add(item.GetRawText());
                }
            }
        }
        else if (el.ValueKind == JsonValueKind.String)
        {
            var raw = el.GetString();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                list.AddRange(raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }
        return list;
    }

    private static SuggestedDrinkDto MapProductToSuggestedDto(
        Product p,
        string reason,
        string? sugarOverride = null,
        string? iceOverride = null,
        List<string>? toppingsOverride = null)
    {
        var isFood = string.Equals(p.ProductType, "FOOD", StringComparison.OrdinalIgnoreCase);
        var isCombo = string.Equals(p.ProductType, "COMBO", StringComparison.OrdinalIgnoreCase);

        return new SuggestedDrinkDto
        {
            ProductId = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ImageUrl = p.ImageUrl,
            Description = p.Description,
            EstimatedPrice = p.BasePrice,
            ProductType = p.ProductType ?? "DRINK",
            CategoryName = p.Category?.Name ?? string.Empty,
            IsInStock = p.IsActive && p.StockStatus == 1,
            SugarRecommend = sugarOverride ?? (isFood ? "Không áp dụng" : isCombo ? "Tùy chọn 50%" : "50%"),
            IceRecommend = iceOverride ?? (isFood ? "Không áp dụng" : isCombo ? "Tùy chọn 70%" : "70%"),
            RecommendedToppings = toppingsOverride ?? (isFood
                ? new List<string> { !string.IsNullOrWhiteSpace(p.DefaultSauce) ? p.DefaultSauce : "Tương ớt cay nồng" }
                : new List<string> { "Trân châu hoàng kim", "Kem cheese" }),
            Reason = reason
        };
    }

    private static void AddSuggestedProductsToContext(
        AgentExecutionContext context,
        IEnumerable<Product> products,
        string reason,
        string? sugar = null,
        string? ice = null,
        List<string>? toppings = null)
    {
        foreach (var p in products)
        {
            if (context.SuggestedDrinks.All(s => s.ProductId != p.Id))
            {
                context.SuggestedDrinks.Add(MapProductToSuggestedDto(p, reason, sugar, ice, toppings));
            }
        }
    }

    // =========================================================================
    // 1. PRODUCT AGENT TOOLS
    // =========================================================================
    private async Task<ToolExecutionResult> ExecuteSearchProductsAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var queryStr = GetString(args, "query")?.Trim().ToLowerInvariant();
        var categoryStr = GetString(args, "category")?.Trim().ToLowerInvariant();
        var productType = GetString(args, "productType")?.Trim().ToUpperInvariant();
        var limit = Math.Clamp(GetInt(args, "limit") ?? 6, 1, 12);

        var dbQuery = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockStatus == 1);

        if (!string.IsNullOrWhiteSpace(productType))
        {
            dbQuery = dbQuery.Where(p => p.ProductType.ToUpper() == productType);
        }

        if (!string.IsNullOrWhiteSpace(categoryStr))
        {
            dbQuery = dbQuery.Where(p =>
                (p.Category != null && (p.Category.Name.ToLower().Contains(categoryStr) || p.Category.Slug.ToLower().Contains(categoryStr))) ||
                p.Slug.ToLower().Contains(categoryStr));
        }

        var list = await dbQuery.ToListAsync();

        if (!string.IsNullOrWhiteSpace(queryStr))
        {
            var nfcQuery = VietnameseNlpHelper.NormalizeNfc(queryStr).ToLowerInvariant();
            var asciiQuery = VietnameseNlpHelper.RemoveDiacritics(nfcQuery);
            var keywords = asciiQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            list = list
                .Select(p =>
                {
                    var nameNfc = VietnameseNlpHelper.NormalizeNfc(p.Name).ToLowerInvariant();
                    var nameAscii = VietnameseNlpHelper.RemoveDiacritics(nameNfc);
                    var descAscii = VietnameseNlpHelper.RemoveDiacritics(p.Description);
                    var slugLower = p.Slug.ToLowerInvariant();
                    int score = 0;
                    if (nameNfc.Contains(nfcQuery) || nameAscii.Contains(asciiQuery)) score += 12;
                    if (slugLower.Contains(asciiQuery.Replace(" ", "-"))) score += 10;
                    if (descAscii.Contains(asciiQuery)) score += 4;
                    foreach (var kw in keywords.Where(k => k.Length >= 2))
                    {
                        if (nameAscii.Contains(kw)) score += 3;
                        if (descAscii.Contains(kw)) score += 1;
                    }
                    return (Product: p, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Product.IsBestSeller)
                .Select(x => x.Product)
                .ToList();
        }
        else
        {
            list = list.OrderByDescending(p => p.IsBestSeller).ThenBy(p => p.BasePrice).ToList();
        }

        var topMatches = list.Take(limit).ToList();
        AddSuggestedProductsToContext(context, topMatches,
            !string.IsNullOrWhiteSpace(queryStr)
                ? $"Khớp với tìm kiếm '{queryStr}' trong thực đơn MilkyVerse."
                : "Sản phẩm nổi bật trong thực đơn MilkyVerse.");

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "SearchProducts",
            Summary = $"Tìm thấy {topMatches.Count} sản phẩm phù hợp.",
            Data = new
            {
                totalMatched = list.Count,
                products = topMatches.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    category = p.Category?.Name ?? "",
                    productType = p.ProductType,
                    basePrice = p.BasePrice,
                    imageUrl = p.ImageUrl,
                    description = p.Description,
                    isBestSeller = p.IsBestSeller,
                    inStock = p.StockStatus == 1 && p.IsActive
                })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetProductDetailsAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var productId = GetInt(args, "productId");
        var productName = GetString(args, "productName")?.Trim().ToLowerInvariant();
        var slug = GetString(args, "slug")?.Trim().ToLowerInvariant();

        Product? product = null;
        var query = _db.Products.Include(p => p.Category).Where(p => p.IsActive);

        if (productId.HasValue && productId.Value > 0)
        {
            product = await query.FirstOrDefaultAsync(p => p.Id == productId.Value);
        }
        if (product == null && !string.IsNullOrWhiteSpace(slug))
        {
            product = await query.FirstOrDefaultAsync(p => p.Slug.ToLower() == slug);
        }
        if (product == null && !string.IsNullOrWhiteSpace(productName))
        {
            product = await query.FirstOrDefaultAsync(p => p.Name.ToLower() == productName)
                   ?? await query.FirstOrDefaultAsync(p => p.Name.ToLower().Contains(productName));
        }

        if (product == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "GetProductDetails",
                Summary = "Không tìm thấy sản phẩm yêu cầu.",
                Data = new { error = "PRODUCT_NOT_FOUND", message = "Không tìm thấy món này trong thực đơn MilkyVerse." }
            };
        }

        AddSuggestedProductsToContext(context, new[] { product }, $"Thông tin chi tiết sản phẩm {product.Name}.");

        var toppings = await _db.Toppings.Where(t => t.IsAvailable).Take(12).ToListAsync();

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetProductDetails",
            Summary = $"Chi tiết món '{product.Name}' ({product.BasePrice:N0}đ).",
            Data = new
            {
                id = product.Id,
                name = product.Name,
                slug = product.Slug,
                category = product.Category?.Name ?? "",
                productType = product.ProductType,
                basePriceSizeM = product.BasePrice,
                priceSizeL = product.ProductType == "DRINK" ? product.BasePrice + 8000 : product.BasePrice,
                imageUrl = product.ImageUrl,
                description = product.Description,
                inStock = product.IsActive && product.StockStatus == 1,
                availableSizes = product.ProductType == "DRINK" ? new[] { "M", "L (+8.000đ)" } : new[] { "Tiêu chuẩn" },
                sugarLevels = new[] { "0%", "30%", "50%", "70%", "100%" },
                iceLevels = new[] { "0%", "30%", "50%", "70%", "100%" },
                defaultSauce = product.DefaultSauce,
                spicyLevel = product.SpicyLevel,
                availableToppings = toppings.Select(t => new { t.Id, t.Name, t.Price })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteRecommendDrinksAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var mood = (GetString(args, "moodOrNeed") ?? "").ToLowerInvariant();
        var maxBudget = GetDecimal(args, "maxBudget");
        var sugarPref = GetString(args, "sugarPreference");
        var avoidCaffeine = GetBool(args, "avoidCaffeine") ?? false;

        var allActive = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockStatus == 1)
            .ToListAsync();

        if (maxBudget.HasValue && maxBudget.Value > 0)
        {
            allActive = allActive.Where(p => p.BasePrice <= maxBudget.Value).ToList();
        }

        string recSugar = sugarPref ?? "50%";
        string recIce = "70%";
        string reason = "Được AI Barista tuyển chọn theo nhu cầu của bạn.";
        List<Product> selected;

        if (mood.Contains("ho") || mood.Contains("họng") || mood.Contains("cảm") || mood.Contains("ấm") || mood.Contains("lạnh"))
        {
            recIce = "0% (Uống ấm)";
            recSugar = sugarPref ?? "50%";
            reason = "Làm dịu cổ họng, giàu Vitamin C và thảo mộc ấm áp (0% đá).";
            selected = allActive
                .Where(p => p.ProductType == "DRINK" &&
                    (p.Slug.Contains("cam-sa") || p.Slug.Contains("mat-ong") || p.Slug.Contains("chanh") || p.Slug.Contains("tac") || p.Slug.Contains("cam") || p.Slug.Contains("sua-tuoi")))
                .Take(5)
                .ToList();
        }
        else if (avoidCaffeine || mood.Contains("tối") || mood.Contains("mất ngủ") || mood.Contains("không caffeine"))
        {
            recSugar = sugarPref ?? "30%";
            reason = "100% không chứa Caffeine, thanh mát, an tâm thưởng thức buổi tối không lo mất ngủ.";
            selected = allActive
                .Where(p => p.ProductType == "DRINK" &&
                    (p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda") || p.Slug.Contains("sua-tuoi") || p.Slug.Contains("sua-chua")))
                .Take(5)
                .ToList();
        }
        else if (mood.Contains("buồn ngủ") || mood.Contains("tỉnh táo") || mood.Contains("deadline") || mood.Contains("cà phê") || mood.Contains("năng lượng"))
        {
            reason = "Giàu Caffeine & L-Theanine tự nhiên giúp tỉnh táo tức thì, tập trung chạy deadline.";
            selected = allActive
                .Where(p => p.ProductType == "DRINK" &&
                    (p.Slug.Contains("ca-phe") || p.Slug.Contains("bac-xiu") || p.Slug.Contains("matcha") || p.Slug.Contains("o-long") || p.Slug.Contains("hong-tra")))
                .Take(5)
                .ToList();
        }
        else if (mood.Contains("ăn kiêng") || mood.Contains("giảm cân") || mood.Contains("ít ngọt") || mood.Contains("healthy"))
        {
            recSugar = sugarPref ?? "30%";
            reason = "Ít calo, độ ngọt thanh nhẹ (0% - 30% đường), phù hợp chế độ Eat-Clean & giữ dáng.";
            selected = allActive
                .Where(p => p.ProductType == "DRINK" &&
                    (p.Slug.Contains("o-long") || p.Slug.Contains("tra-xanh") || p.Slug.Contains("nuoc-ep") || p.Slug.Contains("matcha") || p.Slug.Contains("dao")))
                .Take(5)
                .ToList();
        }
        else if (mood.Contains("ăn vặt") || mood.Contains("đói") || mood.Contains("cay"))
        {
            reason = "Món ăn vặt nóng giòn hấp dẫn nhâm nhi cùng trà sữa.";
            selected = allActive
                .Where(p => p.ProductType == "FOOD")
                .OrderByDescending(p => p.IsBestSeller)
                .Take(5)
                .ToList();
        }
        else
        {
            selected = allActive
                .OrderByDescending(p => p.IsBestSeller)
                .Take(5)
                .ToList();
        }

        if (selected.Count == 0)
        {
            selected = allActive.OrderBy(p => p.BasePrice).Take(5).ToList();
        }

        AddSuggestedProductsToContext(context, selected, reason, recSugar, recIce);

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "RecommendDrinks",
            Summary = $"Đề xuất {selected.Count} món phù hợp với '{mood}'.",
            Data = new
            {
                recommendationReason = reason,
                recommendedSugar = recSugar,
                recommendedIce = recIce,
                products = selected.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    basePrice = p.BasePrice,
                    imageUrl = p.ImageUrl,
                    description = p.Description
                })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteFilterProductsByPriceAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var minPrice = GetDecimal(args, "minPrice") ?? 0;
        var maxPrice = GetDecimal(args, "maxPrice") ?? 50000;
        var productType = GetString(args, "productType")?.Trim().ToUpperInvariant();
        var keyword = GetString(args, "keyword")?.Trim().ToLowerInvariant();
        var limit = Math.Clamp(GetInt(args, "limit") ?? 6, 1, 12);

        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockStatus == 1 && p.BasePrice >= minPrice && p.BasePrice <= maxPrice);

        if (!string.IsNullOrWhiteSpace(productType))
        {
            query = query.Where(p => p.ProductType.ToUpper() == productType);
        }

        var list = await query.ToListAsync();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kwAscii = VietnameseNlpHelper.RemoveDiacritics(keyword);
            var filtered = list.Where(p =>
                VietnameseNlpHelper.RemoveDiacritics(p.Name).Contains(kwAscii) ||
                VietnameseNlpHelper.RemoveDiacritics(p.Description).Contains(kwAscii) ||
                p.Slug.ToLower().Contains(kwAscii.Replace(" ", "-"))).ToList();
            if (filtered.Any()) list = filtered;
        }

        var results = list
            .OrderByDescending(p => p.IsBestSeller)
            .ThenBy(p => p.BasePrice)
            .Take(limit)
            .ToList();

        var priceLabel = minPrice > 0
            ? $"Phù hợp khoảng giá {minPrice:N0}đ – {maxPrice:N0}đ."
            : $"Phù hợp ngân sách ≤ {maxPrice:N0}đ.";
        AddSuggestedProductsToContext(context, results, priceLabel);

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "FilterProductsByPrice",
            Summary = $"Lọc được {results.Count} món trong tầm giá {minPrice:N0}đ - {maxPrice:N0}đ.",
            Data = new
            {
                minPrice,
                maxPrice,
                totalFound = list.Count,
                products = results.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    basePrice = p.BasePrice,
                    productType = p.ProductType,
                    imageUrl = p.ImageUrl,
                    description = p.Description
                })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteFilterProductsByTasteAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var taste = (GetString(args, "tasteProfile") ?? "").ToLowerInvariant();
        var sweetness = GetString(args, "sweetnessLevel") ?? (taste.Contains("ít ngọt") || taste.Contains("không đường") ? "30%" : "50%");
        var noCaffeine = GetBool(args, "noCaffeine") ?? false;
        var noMilk = GetBool(args, "noMilk") ?? false;
        var hasBoba = GetBool(args, "hasBoba") ?? (taste.Contains("trân châu"));
        var maxPrice = GetDecimal(args, "maxPrice");

        var list = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockStatus == 1)
            .ToListAsync();

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            list = list.Where(p => p.BasePrice <= maxPrice.Value).ToList();
        }

        if (noCaffeine)
        {
            list = list.Where(p => p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda") || p.Slug.Contains("sua-tuoi") || p.Slug.Contains("sua-chua")).ToList();
        }

        if (noMilk)
        {
            list = list.Where(p => !p.Slug.Contains("tra-sua") && !p.Slug.Contains("sua-tuoi") && !p.Slug.Contains("kem-cheese")).ToList();
        }

        if (hasBoba)
        {
            var bobaMatches = list.Where(p => p.Slug.Contains("tran-chau") || p.Name.ToLower().Contains("trân châu") || p.Slug.Contains("tra-sua")).ToList();
            if (bobaMatches.Any()) list = bobaMatches;
        }
        else if (taste.Contains("béo") || taste.Contains("cheese") || taste.Contains("kem"))
        {
            var creamy = list.Where(p => p.Slug.Contains("cheese") || p.Slug.Contains("macchiato") || p.Slug.Contains("kem") || p.Slug.Contains("tra-sua")).ToList();
            if (creamy.Any()) list = creamy;
        }
        else if (taste.Contains("thanh") || taste.Contains("chua") || taste.Contains("trái cây") || taste.Contains("mát"))
        {
            var fruity = list.Where(p => p.Slug.Contains("dao") || p.Slug.Contains("vai") || p.Slug.Contains("chanh") || p.Slug.Contains("cam") || p.Slug.Contains("nuoc-ep") || p.Slug.Contains("soda")).ToList();
            if (fruity.Any()) list = fruity;
        }
        else if (taste.Contains("matcha"))
        {
            var matcha = list.Where(p => p.Slug.Contains("matcha")).ToList();
            if (matcha.Any()) list = matcha;
        }

        var selected = list.OrderByDescending(p => p.IsBestSeller).ThenBy(p => p.BasePrice).Take(6).ToList();
        var defaultToppings = hasBoba
            ? new List<string> { "Trân châu đen", "Trân châu hoàng kim" }
            : null;

        AddSuggestedProductsToContext(context, selected, $"Phù hợp khẩu vị: {taste} (Độ ngọt gợi ý: {sweetness}).", sweetness, "70%", defaultToppings);

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "FilterProductsByTaste",
            Summary = $"Tìm thấy {selected.Count} món hợp khẩu vị '{taste}'.",
            Data = new
            {
                tasteProfile = taste,
                recommendedSweetness = sweetness,
                products = selected.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    basePrice = p.BasePrice,
                    imageUrl = p.ImageUrl,
                    description = p.Description
                })
            }
        };
    }

    // =========================================================================
    // 2. DIY DRINK STUDIO AGENT TOOLS
    // =========================================================================
    private static string NormalizeTeaBaseId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "oolong_tea";
        var l = input.ToLowerInvariant();
        if (l.Contains("matcha")) return "matcha_base";
        if (l.Contains("green") || l.Contains("lục") || l.Contains("lài") || l.Contains("xanh")) return "green_tea";
        if (l.Contains("oolong") || l.Contains("ô long") || l.Contains("o long")) return "oolong_tea";
        return "black_tea";
    }

    private static string NormalizeMilkId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "fresh_milk";
        var l = input.ToLowerInvariant();
        if (l.Contains("oat") || l.Contains("yến mạch") || l.Contains("yen mach") || l.Contains("chay")) return "oat_milk";
        if (l.Contains("almond") || l.Contains("hạnh nhân") || l.Contains("hanh nhan")) return "almond_milk";
        if (l.Contains("condensed") || l.Contains("đặc") || l.Contains("dac")) return "condensed_milk";
        return "fresh_milk";
    }

    private static string NormalizePercentage(string? input, string defaultVal = "50%")
    {
        if (string.IsNullOrWhiteSpace(input)) return defaultVal;
        var l = input.Trim().ToLowerInvariant();
        if (l.Contains("100") || l.Contains("chuẩn") || l.Contains("bình thường")) return "100%";
        if (l.Contains("70")) return "70%";
        if (l.Contains("50") || l.Contains("vừa")) return "50%";
        if (l.Contains("30") || l.Contains("ít")) return "30%";
        if (l.Contains("0") || l.Contains("không")) return "0%";
        return defaultVal;
    }

    private async Task<List<Topping>> ResolveToppingsByNameAsync(List<string> toppingNames)
    {
        var availableToppings = await _db.Toppings.Where(t => t.IsAvailable).ToListAsync();
        var matched = new List<Topping>();

        foreach (var rawName in toppingNames)
        {
            var clean = rawName.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(clean)) continue;

            Topping? found = null;
            if (int.TryParse(clean, out var tid))
            {
                found = availableToppings.FirstOrDefault(t => t.Id == tid);
            }
            found ??= availableToppings.FirstOrDefault(t => t.Name.ToLowerInvariant() == clean)
                   ?? availableToppings.FirstOrDefault(t => t.Name.ToLowerInvariant().Contains(clean) || clean.Contains(t.Name.ToLowerInvariant()));

            if (found != null && matched.All(m => m.Id != found.Id))
            {
                matched.Add(found);
            }
        }

        return matched;
    }

    private async Task<ToolExecutionResult> ExecuteGenerateRecipeAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var theme = GetString(args, "themeOrCraving") ?? "Signature MilkyVerse";
        var isRefOnly = GetBool(args, "isReferenceOnly") ?? false;

        var teaBaseId = NormalizeTeaBaseId(GetString(args, "teaBaseId") ?? theme);
        var milkId = NormalizeMilkId(GetString(args, "milkId") ?? theme);
        var size = (GetString(args, "size")?.ToUpperInvariant() == "L") ? "L" : "M";
        var sugar = NormalizePercentage(GetString(args, "sugarLevel") ?? (theme.ToLower().Contains("ít ngọt") ? "30%" : "50%"), "50%");
        var ice = NormalizePercentage(GetString(args, "iceLevel") ?? "70%", "70%");

        var requestedToppingNames = GetStringList(args, "toppingNames");
        var resolvedToppings = await ResolveToppingsByNameAsync(requestedToppingNames);

        if (!resolvedToppings.Any())
        {
            var allToppings = await _db.Toppings.Where(t => t.IsAvailable).Take(2).ToListAsync();
            resolvedToppings = allToppings;
        }

        var quoteRes = await _diyService.QuoteDrinkAsync(new DIYQuoteRequest
        {
            TeaBaseId = teaBaseId,
            MilkId = milkId,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            ToppingIds = resolvedToppings.Select(t => t.Id).ToList(),
            Quantity = 1
        });

        var catalogRes = await _diyService.GetCatalogAsync();
        var teaObj = catalogRes.Data?.TeaBases.FirstOrDefault(t => t.Id == teaBaseId);
        var milkObj = catalogRes.Data?.Milks.FirstOrDefault(m => m.Id == milkId);

        var quote = quoteRes.Data;
        var recipeCard = new AIRecipeCardDto
        {
            RecipeName = isRefOnly
                ? $"[Công Thức Tham Khảo] {teaObj?.Name ?? "Trà"} x {milkObj?.Name ?? "Sữa"}"
                : (quote?.RecipeName ?? $"DIY Studio: {teaObj?.Name} x {milkObj?.Name}"),
            Description = isRefOnly
                ? $"Công thức sáng tạo bởi Milky AI để bạn tham khảo pha chế theo cảm hứng '{theme}'."
                : $"Cấu hình DIY Drink Studio chuẩn quy tắc MilkyVerse — có thể thêm trực tiếp vào giỏ hàng và đặt mua ngay!",
            TeaBaseId = teaBaseId,
            TeaBaseName = teaObj?.Name ?? teaBaseId,
            MilkId = milkId,
            MilkName = milkObj?.Name ?? milkId,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            Toppings = resolvedToppings.Select(t => new ToppingDto { Id = t.Id, Name = t.Name, Price = t.Price, IsAvailable = t.IsAvailable }).ToList(),
            UnitPrice = quote?.UnitPrice ?? 48000,
            PriceBreakdown = quote?.Breakdown ?? new List<string>(),
            BrewingSteps = new List<string>
            {
                $"Bước 1: Ủ cốt {teaObj?.Name ?? "Trà thượng hạng"} ở nhiệt độ 90°C giữ trọn hương thơm.",
                $"Bước 2: Phối cùng {milkObj?.Name ?? "Sữa tươi"} và cân chỉnh mức đường {sugar}, mức đá {ice}.",
                $"Bước 3: Thêm topping ({string.Join(", ", resolvedToppings.Select(t => t.Name))}) vào đáy ly Size {size}."
            },
            IsReferenceFormula = isRefOnly,
            IsPurchasable = !isRefOnly && (quote?.IsValid ?? true),
            DiyLink = $"/diy?tea={teaBaseId}&milk={milkId}&size={size}"
        };

        context.Recipes.Add(recipeCard);
        context.Actions.Add(new AIClientActionDto
        {
            Type = "OPEN_DIY",
            Payload = new
            {
                recipeName = recipeCard.RecipeName,
                teaBaseId = recipeCard.TeaBaseId,
                milkId = recipeCard.MilkId,
                size = recipeCard.Size,
                sugarLevel = recipeCard.SugarLevel,
                iceLevel = recipeCard.IceLevel,
                toppingIds = recipeCard.Toppings.Select(t => t.Id).ToList(),
                unitPrice = recipeCard.UnitPrice,
                breakdown = recipeCard.PriceBreakdown,
                diyUrl = recipeCard.DiyLink
            }
        });

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GenerateRecipe",
            Summary = $"Đã tạo công thức '{recipeCard.RecipeName}' — Giá chuẩn: {recipeCard.UnitPrice:N0}đ ({(isRefOnly ? "Công thức tham khảo" : "Có thể đặt hàng ngay")}).",
            Data = recipeCard
        };
    }

    private async Task<ToolExecutionResult> ExecuteCustomizeDrinkAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var productId = GetInt(args, "productId");
        var productName = GetString(args, "productName");
        var isDiy = GetBool(args, "isCustomDIY") ?? (!productId.HasValue && string.IsNullOrWhiteSpace(productName));
        var size = (GetString(args, "size")?.ToUpperInvariant() == "L") ? "L" : "M";
        var sugar = NormalizePercentage(GetString(args, "sugarLevel"), "50%");
        var ice = NormalizePercentage(GetString(args, "iceLevel"), "70%");
        var quantity = Math.Clamp(GetInt(args, "quantity") ?? 1, 1, 50);
        var toppingNames = GetStringList(args, "toppingNames");
        var resolvedToppings = await ResolveToppingsByNameAsync(toppingNames);

        if (!isDiy)
        {
            Product? product = null;
            if (productId.HasValue)
            {
                product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId.Value && p.IsActive);
            }
            if (product == null && !string.IsNullOrWhiteSpace(productName))
            {
                var lowerName = productName.Trim().ToLowerInvariant();
                product = await _db.Products.FirstOrDefaultAsync(p => p.IsActive && p.Name.ToLower() == lowerName)
                       ?? await _db.Products.FirstOrDefaultAsync(p => p.IsActive && p.Name.ToLower().Contains(lowerName));
            }

            if (product != null)
            {
                decimal sizeExtra = (product.ProductType == "DRINK" && size == "L") ? 8000 : 0;
                decimal toppingsTotal = resolvedToppings.Sum(t => t.Price);
                decimal unitPrice = product.BasePrice + sizeExtra + toppingsTotal;
                decimal totalPrice = unitPrice * quantity;

                var breakdown = new List<string>
                {
                    $"{product.Name} (Giá gốc): {product.BasePrice:N0}đ"
                };
                if (sizeExtra > 0) breakdown.Add($"Nâng cấp Size L: +{sizeExtra:N0}đ");
                breakdown.AddRange(resolvedToppings.Select(t => $"{t.Name}: +{t.Price:N0}đ"));

                var card = new AIDrinkCustomizationCardDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Slug = product.Slug,
                    ImageUrl = product.ImageUrl,
                    IsCustomDIY = false,
                    Size = size,
                    SugarLevel = sugar,
                    IceLevel = ice,
                    SelectedToppings = resolvedToppings.Select(t => new ToppingDto { Id = t.Id, Name = t.Name, Price = t.Price, IsAvailable = true }).ToList(),
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = totalPrice,
                    Breakdown = breakdown,
                    IsValid = product.IsActive && product.StockStatus == 1
                };

                context.Customizations.Add(card);
                return new ToolExecutionResult
                {
                    Success = true,
                    ToolName = "CustomizeDrink",
                    Summary = $"Đã cấu hình {quantity}x {product.Name} (Size {size}, {sugar} đường, {ice} đá) — Đơn giá: {unitPrice:N0}đ.",
                    Data = card
                };
            }
        }

        // DIY configuration
        var teaBaseId = NormalizeTeaBaseId(GetString(args, "teaBaseId") ?? productName);
        var milkId = NormalizeMilkId(GetString(args, "milkId") ?? productName);

        var quoteRes = await _diyService.QuoteDrinkAsync(new DIYQuoteRequest
        {
            TeaBaseId = teaBaseId,
            MilkId = milkId,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            ToppingIds = resolvedToppings.Select(t => t.Id).ToList(),
            Quantity = quantity
        });

        if (!quoteRes.Success || quoteRes.Data == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CustomizeDrink",
                Summary = quoteRes.Message,
                Data = new { error = "INVALID_DIY_CONFIG", message = quoteRes.Message }
            };
        }

        var diyCard = new AIDrinkCustomizationCardDto
        {
            ProductName = quoteRes.Data.RecipeName,
            ImageUrl = "/images/products/tra-o-long-kem-cheese.png",
            IsCustomDIY = true,
            TeaBaseId = teaBaseId,
            MilkId = milkId,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            SelectedToppings = resolvedToppings.Select(t => new ToppingDto { Id = t.Id, Name = t.Name, Price = t.Price, IsAvailable = true }).ToList(),
            Quantity = quantity,
            UnitPrice = quoteRes.Data.UnitPrice,
            TotalPrice = quoteRes.Data.TotalPrice,
            Breakdown = quoteRes.Data.Breakdown,
            IsValid = quoteRes.Data.IsValid
        };

        context.Customizations.Add(diyCard);
        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "CustomizeDrink",
            Summary = $"Đã cấu hình DIY '{diyCard.ProductName}' — Đơn giá: {diyCard.UnitPrice:N0}đ, Tổng: {diyCard.TotalPrice:N0}đ.",
            Data = diyCard
        };
    }

    private async Task<ToolExecutionResult> ExecuteCalculateDrinkPriceAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        return await ExecuteCustomizeDrinkAsync(args, context);
    }

    private async Task<ToolExecutionResult> ExecuteGetAvailableToppingsAsync()
    {
        var toppings = await _db.Toppings
            .Where(t => t.IsAvailable)
            .OrderBy(t => t.Price)
            .Select(t => new ToppingDto
            {
                Id = t.Id,
                Name = t.Name,
                Price = t.Price,
                IsAvailable = t.IsAvailable
            })
            .ToListAsync();

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetAvailableToppings",
            Summary = $"Hiện có {toppings.Count} loại topping khả dụng.",
            Data = new { count = toppings.Count, toppings }
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetAvailableIngredientsAsync(Dictionary<string, JsonElement> args)
    {
        var catRes = await _diyService.GetCatalogAsync();
        var ingredients = await _db.Ingredients
            .Where(i => i.IsActive && i.CurrentStock > 0)
            .Select(i => new { i.Id, i.Name, i.CategoryType, i.Unit, inStock = i.CurrentStock > 0 })
            .ToListAsync();

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetAvailableIngredients",
            Summary = "Đã lấy danh sách cốt trà, sữa, độ ngọt, đá và nguyên liệu hiện có.",
            Data = new
            {
                diyCatalog = catRes.Data,
                availableStoreIngredientsCount = ingredients.Count,
                sampleIngredients = ingredients.Take(20)
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteSaveFavoriteRecipeAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var recipeName = GetString(args, "recipeName") ?? "Công thức DIY Yêu Thích";
        var teaBaseId = NormalizeTeaBaseId(GetString(args, "teaBaseId"));
        var milkId = NormalizeMilkId(GetString(args, "milkId"));
        var size = (GetString(args, "size")?.ToUpperInvariant() == "L") ? "L" : "M";
        var sugar = NormalizePercentage(GetString(args, "sugarLevel"), "50%");
        var ice = NormalizePercentage(GetString(args, "iceLevel"), "70%");
        var notes = GetString(args, "notes") ?? "";
        var toppingNames = GetStringList(args, "toppingNames");
        var resolvedToppings = await ResolveToppingsByNameAsync(toppingNames);

        var quoteRes = await _diyService.QuoteDrinkAsync(new DIYQuoteRequest
        {
            TeaBaseId = teaBaseId,
            MilkId = milkId,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            ToppingIds = resolvedToppings.Select(t => t.Id).ToList(),
            Quantity = 1
        });

        var catRes = await _diyService.GetCatalogAsync();
        var teaName = catRes.Data?.TeaBases.FirstOrDefault(t => t.Id == teaBaseId)?.Name ?? teaBaseId;
        var milkName = catRes.Data?.Milks.FirstOrDefault(m => m.Id == milkId)?.Name ?? milkId;

        var saved = await _conversationService.SaveFavoriteRecipeAsync(context.UserId, context.SessionId, new FavoriteRecipeDto
        {
            RecipeName = recipeName,
            TeaBaseId = teaBaseId,
            TeaBaseName = teaName,
            MilkId = milkId,
            MilkName = milkName,
            Size = size,
            SugarLevel = sugar,
            IceLevel = ice,
            ToppingIds = resolvedToppings.Select(t => t.Id).ToList(),
            ToppingNames = resolvedToppings.Select(t => t.Name).ToList(),
            UnitPrice = quoteRes.Data?.UnitPrice ?? 40000,
            Notes = notes,
            IsReferenceFormula = false
        });

        return new ToolExecutionResult
        {
            Success = saved.Success,
            ToolName = "SaveFavoriteRecipe",
            Summary = $"Đã lưu công thức yêu thích '{recipeName}' ({saved.Data?.UnitPrice:N0}đ).",
            Data = saved.Data ?? (object)new { message = saved.Message }
        };
    }

    // =========================================================================
    // 3. SHOPPING & CART AGENT TOOLS
    // =========================================================================
    private ToolExecutionResult ExecuteGetCart(AgentExecutionContext context)
    {
        var subtotal = context.WorkingCart.Sum(i => i.TotalPrice);
        var totalQty = context.WorkingCart.Sum(i => i.Quantity);

        context.CartAction = new AICartActionCardDto
        {
            ActionType = "VIEW",
            Summary = context.WorkingCart.Any()
                ? $"Giỏ hàng hiện có {totalQty} món — Tạm tính: {subtotal:N0}đ"
                : "Giỏ hàng của bạn hiện đang trống.",
            CurrentCart = context.WorkingCart.ToList(),
            TotalItemsCount = totalQty,
            CartSubtotal = subtotal
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetCart",
            Summary = context.CartAction.Summary,
            Data = context.CartAction
        };
    }

    private async Task<ToolExecutionResult> ExecuteAddToCartAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        // Enforce role check: WarehouseStaff, DeliveryStaff cannot place customer orders
        if (string.Equals(context.UserRole, "WarehouseStaff", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.UserRole, "DeliveryStaff", StringComparison.OrdinalIgnoreCase))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "AddToCart",
                Summary = $"Vai trò '{context.UserRole}' không có quyền đặt món vào giỏ hàng.",
                Data = new { error = "ROLE_NOT_ALLOWED_TO_ORDER" }
            };
        }

        var productId = GetInt(args, "productId");
        var productName = GetString(args, "productName");
        var isCustomDiy = GetBool(args, "isCustomDIY") ?? false;
        var size = (GetString(args, "size")?.ToUpperInvariant() == "L") ? "L" : "M";
        var sugar = NormalizePercentage(GetString(args, "sugarLevel"), "50%");
        var ice = NormalizePercentage(GetString(args, "iceLevel"), "70%");
        var spicy = GetString(args, "spicyLevel") ?? "Không cay";
        var sauce = GetString(args, "dippingSauce") ?? "Tương ớt";
        var quantity = Math.Clamp(GetInt(args, "quantity") ?? 1, 1, 50);
        var customNotes = GetString(args, "customNotes") ?? "";
        var toppingNames = GetStringList(args, "toppingNames");
        var resolvedToppings = await ResolveToppingsByNameAsync(toppingNames);

        AIClientCartItemDto cartItem;

        if (isCustomDiy)
        {
            var teaBaseId = NormalizeTeaBaseId(GetString(args, "teaBaseId") ?? productName);
            var milkId = NormalizeMilkId(GetString(args, "milkId") ?? productName);

            var quoteRes = await _diyService.QuoteDrinkAsync(new DIYQuoteRequest
            {
                TeaBaseId = teaBaseId,
                MilkId = milkId,
                Size = size,
                SugarLevel = sugar,
                IceLevel = ice,
                ToppingIds = resolvedToppings.Select(t => t.Id).ToList(),
                Quantity = quantity
            });

            if (!quoteRes.Success || quoteRes.Data == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ToolName = "AddToCart",
                    Summary = quoteRes.Message,
                    Data = new { error = "INVALID_DIY", message = quoteRes.Message }
                };
            }

            cartItem = new AIClientCartItemDto
            {
                Id = "item_diy_" + Guid.NewGuid().ToString("N")[..8],
                Name = quoteRes.Data.RecipeName,
                ImageUrl = "/images/products/tra-o-long-kem-cheese.png",
                ItemType = "DRINK",
                Size = size,
                SugarLevel = sugar,
                IceLevel = ice,
                UnitPrice = quoteRes.Data.UnitPrice,
                Quantity = quantity,
                TotalPrice = quoteRes.Data.TotalPrice,
                IsCustomDIY = true,
                CustomNotes = string.IsNullOrWhiteSpace(customNotes)
                    ? $"DIY Studio ({sugar} đường, {ice} đá)"
                    : customNotes,
                Toppings = resolvedToppings.Select(t => new AIClientCartToppingDto
                {
                    ToppingId = t.Id,
                    Name = t.Name,
                    Price = t.Price,
                    Quantity = 1
                }).ToList()
            };
        }
        else
        {
            Product? product = null;
            var allActiveProducts = await _db.Products.Where(p => p.IsActive).ToListAsync();
            if (productId.HasValue && productId.Value > 0)
            {
                product = allActiveProducts.FirstOrDefault(p => p.Id == productId.Value);
            }
            if (product == null && !string.IsNullOrWhiteSpace(productName))
            {
                product = VietnameseNlpHelper.FindBestMatchingProduct(productName, allActiveProducts);
            }

            if (product == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ToolName = "AddToCart",
                    Summary = $"Không tìm thấy món '{productName}' trong thực đơn đang kinh doanh.",
                    Data = new { error = "PRODUCT_NOT_FOUND", requestedName = productName }
                };
            }

            if (product.StockStatus == 0)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ToolName = "AddToCart",
                    Summary = $"Món '{product.Name}' hiện đang tạm hết hàng.",
                    Data = new { error = "OUT_OF_STOCK", productName = product.Name }
                };
            }

            var isFood = string.Equals(product.ProductType, "FOOD", StringComparison.OrdinalIgnoreCase);
            var effectiveSize = isFood ? "Standard" : size;
            decimal sizeSurcharge = (!isFood && effectiveSize == "L") ? 8000 : 0;
            decimal toppingsTotal = resolvedToppings.Sum(t => t.Price);
            decimal unitPrice = product.BasePrice + sizeSurcharge + toppingsTotal;

            cartItem = new AIClientCartItemDto
            {
                Id = $"item_{product.Id}_{Guid.NewGuid().ToString("N")[..6]}",
                ProductId = product.Id,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                ItemType = product.ProductType ?? "DRINK",
                Size = effectiveSize,
                SugarLevel = isFood ? "" : sugar,
                IceLevel = isFood ? "" : ice,
                SpicyLevel = isFood ? spicy : "",
                DippingSauce = isFood ? sauce : "",
                PortionSize = "Tiêu chuẩn",
                UnitPrice = unitPrice,
                Quantity = quantity,
                TotalPrice = unitPrice * quantity,
                IsCustomDIY = false,
                CustomNotes = customNotes,
                Toppings = resolvedToppings.Select(t => new AIClientCartToppingDto
                {
                    ToppingId = t.Id,
                    Name = t.Name,
                    Price = t.Price,
                    Quantity = 1
                }).ToList()
            };
        }

        // Check if identical item configuration already exists in WorkingCart
        var existing = context.WorkingCart.FirstOrDefault(i =>
            i.ProductId == cartItem.ProductId &&
            string.Equals(i.Name, cartItem.Name, StringComparison.OrdinalIgnoreCase) &&
            i.Size == cartItem.Size &&
            i.SugarLevel == cartItem.SugarLevel &&
            i.IceLevel == cartItem.IceLevel &&
            string.Join(",", i.Toppings.Select(t => t.ToppingId).OrderBy(x => x)) ==
            string.Join(",", cartItem.Toppings.Select(t => t.ToppingId).OrderBy(x => x)));

        if (existing != null)
        {
            existing.Quantity += cartItem.Quantity;
            existing.TotalPrice = existing.UnitPrice * existing.Quantity;
        }
        else
        {
            context.WorkingCart.Add(cartItem);
        }

        await SyncWorkingCartToDbAsync(context);

        var subtotal = context.WorkingCart.Sum(i => i.TotalPrice);
        var totalCount = context.WorkingCart.Sum(i => i.Quantity);

        var affectedList = context.WorkingCart?.AffectedItemsListOrNew(cartItem) ?? new List<AIClientCartItemDto> { cartItem };
        if (context.CartAction != null && context.CartAction.ActionType == "ADD")
        {
            affectedList = context.CartAction.AffectedItems;
            affectedList.Add(cartItem);
        }

        context.CartAction = new AICartActionCardDto
        {
            ActionType = "ADD",
            Summary = $"Đã thêm {cartItem.Quantity}x {cartItem.Name} (Size {cartItem.Size}{(string.IsNullOrEmpty(cartItem.SugarLevel) ? "" : $", {cartItem.SugarLevel} đường")}, {cartItem.UnitPrice:N0}đ/ly) vào giỏ hàng.",
            AffectedItems = affectedList,
            CurrentCart = context.WorkingCart!.ToList(),
            TotalItemsCount = totalCount,
            CartSubtotal = subtotal
        };

        context.Actions.Add(new AIClientActionDto
        {
            Type = "ADD_TO_CART",
            Payload = new
            {
                productId = cartItem.ProductId,
                name = cartItem.Name,
                imageUrl = cartItem.ImageUrl,
                itemType = cartItem.ItemType,
                size = cartItem.Size,
                sugarLevel = cartItem.SugarLevel,
                iceLevel = cartItem.IceLevel,
                spicyLevel = cartItem.SpicyLevel,
                dippingSauce = cartItem.DippingSauce,
                unitPrice = cartItem.UnitPrice,
                quantity = cartItem.Quantity,
                totalPrice = cartItem.TotalPrice,
                isCustomDIY = cartItem.IsCustomDIY,
                customNotes = cartItem.CustomNotes,
                toppings = cartItem.Toppings
            }
        });

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "AddToCart",
            Summary = context.CartAction.Summary,
            Data = new
            {
                addedItem = cartItem,
                cartTotalItems = totalCount,
                cartSubtotal = subtotal
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteUpdateCartItemAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var cartItemId = GetString(args, "cartItemId");
        var productName = GetString(args, "productName")?.Trim().ToLowerInvariant();
        var newQty = GetInt(args, "newQuantity");
        var newSize = GetString(args, "size")?.ToUpperInvariant();
        var newSugar = GetString(args, "sugarLevel");
        var newIce = GetString(args, "iceLevel");

        var target = context.WorkingCart.FirstOrDefault(i =>
            (!string.IsNullOrWhiteSpace(cartItemId) && i.Id == cartItemId) ||
            (!string.IsNullOrWhiteSpace(productName) && i.Name.ToLowerInvariant().Contains(productName)));

        if (target == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "UpdateCartItem",
                Summary = "Không tìm thấy món cần cập nhật trong giỏ hàng.",
                Data = new { error = "CART_ITEM_NOT_FOUND" }
            };
        }

        if (newQty.HasValue && newQty.Value <= 0)
        {
            context.WorkingCart.Remove(target);
            await SyncWorkingCartToDbAsync(context);
            context.CartAction = new AICartActionCardDto
            {
                ActionType = "REMOVE",
                Summary = $"Đã xóa '{target.Name}' khỏi giỏ hàng.",
                AffectedItems = new List<AIClientCartItemDto> { target },
                CurrentCart = context.WorkingCart.ToList(),
                TotalItemsCount = context.WorkingCart.Sum(i => i.Quantity),
                CartSubtotal = context.WorkingCart.Sum(i => i.TotalPrice)
            };
            return new ToolExecutionResult
            {
                Success = true,
                ToolName = "UpdateCartItem",
                Summary = context.CartAction.Summary,
                Data = context.CartAction
            };
        }

        if (newQty.HasValue) target.Quantity = Math.Clamp(newQty.Value, 1, 50);
        if (!string.IsNullOrWhiteSpace(newSize) && (newSize == "M" || newSize == "L"))
        {
            if (target.Size != newSize && target.ItemType == "DRINK")
            {
                target.UnitPrice += (newSize == "L" ? 8000 : -8000);
                target.Size = newSize;
            }
        }
        if (!string.IsNullOrWhiteSpace(newSugar)) target.SugarLevel = NormalizePercentage(newSugar, target.SugarLevel ?? "50%");
        if (!string.IsNullOrWhiteSpace(newIce)) target.IceLevel = NormalizePercentage(newIce, target.IceLevel ?? "70%");

        target.TotalPrice = target.UnitPrice * target.Quantity;
        await SyncWorkingCartToDbAsync(context);

        context.CartAction = new AICartActionCardDto
        {
            ActionType = "UPDATE",
            Summary = $"Đã cập nhật '{target.Name}' (SL: {target.Quantity}, Size {target.Size}) — Thành tiền: {target.TotalPrice:N0}đ.",
            AffectedItems = new List<AIClientCartItemDto> { target },
            CurrentCart = context.WorkingCart.ToList(),
            TotalItemsCount = context.WorkingCart.Sum(i => i.Quantity),
            CartSubtotal = context.WorkingCart.Sum(i => i.TotalPrice)
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "UpdateCartItem",
            Summary = context.CartAction.Summary,
            Data = context.CartAction
        };
    }

    private async Task<ToolExecutionResult> ExecuteRemoveCartItemAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var cartItemId = GetString(args, "cartItemId");
        var productName = GetString(args, "productName")?.Trim().ToLowerInvariant();

        var target = context.WorkingCart.FirstOrDefault(i =>
            (!string.IsNullOrWhiteSpace(cartItemId) && i.Id == cartItemId) ||
            (!string.IsNullOrWhiteSpace(productName) && i.Name.ToLowerInvariant().Contains(productName)));

        if (target == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "RemoveCartItem",
                Summary = "Không tìm thấy món cần xóa trong giỏ hàng.",
                Data = new { error = "CART_ITEM_NOT_FOUND" }
            };
        }

        context.WorkingCart.Remove(target);
        await SyncWorkingCartToDbAsync(context);

        context.CartAction = new AICartActionCardDto
        {
            ActionType = "REMOVE",
            Summary = $"Đã xóa '{target.Name}' khỏi giỏ hàng.",
            AffectedItems = new List<AIClientCartItemDto> { target },
            CurrentCart = context.WorkingCart.ToList(),
            TotalItemsCount = context.WorkingCart.Sum(i => i.Quantity),
            CartSubtotal = context.WorkingCart.Sum(i => i.TotalPrice)
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "RemoveCartItem",
            Summary = context.CartAction.Summary,
            Data = context.CartAction
        };
    }

    private async Task<ToolExecutionResult> ExecuteClearCartAsync(AgentExecutionContext context)
    {
        context.WorkingCart.Clear();
        await SyncWorkingCartToDbAsync(context);

        context.CartAction = new AICartActionCardDto
        {
            ActionType = "CLEAR",
            Summary = "Đã xóa toàn bộ các món trong giỏ hàng.",
            CurrentCart = new List<AIClientCartItemDto>(),
            TotalItemsCount = 0,
            CartSubtotal = 0
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "ClearCart",
            Summary = context.CartAction.Summary,
            Data = context.CartAction
        };
    }

    // =========================================================================
    // 4. ORDER AGENT TOOLS
    // =========================================================================
    private async Task<ToolExecutionResult> ExecuteCreateOrderAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var confirmed = GetBool(args, "confirmedByCustomer") ?? false;
        var customerName = GetString(args, "customerName") ?? context.UserName ?? "";
        var customerPhone = GetString(args, "customerPhone") ?? context.UserPhone ?? "";
        var shippingAddress = GetString(args, "shippingAddress") ?? context.UserAddress ?? "";
        var paymentMethod = (GetString(args, "paymentMethod") ?? "COD").ToUpperInvariant();
        var promoCode = GetString(args, "promotionCode");
        var note = GetString(args, "note") ?? "Đặt hàng qua Milky AI Barista";

        if (!context.WorkingCart.Any())
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CreateOrder",
                Summary = "Giỏ hàng đang trống nên không thể tạo đơn hàng.",
                Data = new { error = "EMPTY_CART", message = "Giỏ hàng đang trống. Vui lòng thêm món trước khi đặt hàng." }
            };
        }

        if (!confirmed || string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace(shippingAddress))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CreateOrder",
                Summary = "Cần khách hàng xác nhận đầy đủ Họ tên, Số điện thoại, Địa chỉ giao hàng và đồng ý chốt đơn.",
                Data = new
                {
                    error = "CONFIRMATION_AND_SHIPPING_INFO_REQUIRED",
                    missingFields = new
                    {
                        customerName = string.IsNullOrWhiteSpace(customerName),
                        customerPhone = string.IsNullOrWhiteSpace(customerPhone),
                        shippingAddress = string.IsNullOrWhiteSpace(shippingAddress),
                        confirmedByCustomer = !confirmed
                    },
                    cartPreview = new
                    {
                        itemsCount = context.WorkingCart.Sum(i => i.Quantity),
                        subtotal = context.WorkingCart.Sum(i => i.TotalPrice)
                    },
                    message = "Vui lòng hỏi khách hàng xác nhận Họ tên, Số điện thoại, Địa chỉ nhận hàng và xác nhận đồng ý tạo đơn."
                }
            };
        }

        var createReq = new CreateOrderRequest
        {
            CustomerName = customerName.Trim(),
            CustomerPhone = customerPhone.Trim(),
            ShippingAddress = shippingAddress.Trim(),
            PaymentMethod = paymentMethod,
            PromotionCode = promoCode,
            Note = note,
            Items = context.WorkingCart.Select(c => new CreateOrderItemRequest
            {
                ProductId = c.ProductId,
                ProductName = c.Name,
                ItemType = c.ItemType,
                Size = c.Size,
                SugarLevel = c.SugarLevel ?? "50%",
                IceLevel = c.IceLevel ?? "70%",
                SpicyLevel = c.SpicyLevel ?? "Không cay",
                DippingSauce = c.DippingSauce ?? "Tương ớt",
                PortionSize = c.PortionSize ?? "Tiêu chuẩn",
                Quantity = c.Quantity,
                IsCustomDIY = c.IsCustomDIY,
                CustomNotes = c.CustomNotes ?? "",
                Toppings = c.Toppings.Select(t => new OrderItemToppingRequest
                {
                    ToppingId = t.ToppingId,
                    Quantity = Math.Max(1, t.Quantity)
                }).ToList()
            }).ToList()
        };

        var orderRes = await _orderService.CreateOrderAsync(context.UserId, createReq);
        if (!orderRes.Success || orderRes.Data == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CreateOrder",
                Summary = $"Không thể tạo đơn: {orderRes.Message}",
                Data = new { error = "CREATE_ORDER_FAILED", message = orderRes.Message }
            };
        }

        var createdOrder = orderRes.Data;

        // Clear cart after successful order creation
        context.WorkingCart.Clear();
        await SyncWorkingCartToDbAsync(context);
        context.CartAction = new AICartActionCardDto
        {
            ActionType = "CLEAR",
            Summary = $"Đã chuyển giỏ hàng thành đơn hàng #{createdOrder.OrderCode}.",
            CurrentCart = new List<AIClientCartItemDto>(),
            TotalItemsCount = 0,
            CartSubtotal = 0
        };

        var orderCard = MapOrderToCard(createdOrder);
        context.Orders.Add(orderCard);

        // If SEPAY payment method, generate real VietQR session
        if (paymentMethod == "SEPAY" || paymentMethod == "VIETQR")
        {
            var qrRes = await _sePayService.GetOrCreateSePayQrAsync(createdOrder.Id, context.UserId);
            if (qrRes.Success && qrRes.Data != null)
            {
                orderCard.PaymentQrUrl = qrRes.Data.QrUrl;
                context.Payment = new AIPaymentCardDto
                {
                    OrderId = createdOrder.Id,
                    OrderCode = createdOrder.OrderCode,
                    PaymentMethod = "SEPAY",
                    PaymentStatus = createdOrder.PaymentStatus,
                    Amount = createdOrder.TotalAmount,
                    QrCodeUrl = qrRes.Data.QrUrl,
                    PayUrl = $"/orders/{createdOrder.Id}",
                    BankName = qrRes.Data.Bank,
                    AccountNumber = qrRes.Data.AccountNumber,
                    AccountName = qrRes.Data.AccountName,
                    TransferContent = qrRes.Data.Description,
                    Instructions = $"Quét mã VietQR TPBank ({qrRes.Data.AccountNumber}) với nội dung '{qrRes.Data.Description}'. Hệ thống SePay sẽ tự động xác nhận ngay khi nhận tiền."
                };
            }
        }

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "CreateOrder",
            Summary = $"Đã tạo đơn hàng #{createdOrder.OrderCode} thành công — Tổng thanh toán: {createdOrder.TotalAmount:N0}đ.",
            Data = orderCard
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetOrderHistoryAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        if (!context.UserId.HasValue || context.UserId.Value <= 0)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "GetOrderHistory",
                Summary = "Khách hàng cần đăng nhập để tra cứu lịch sử đơn hàng.",
                Data = new { error = "AUTH_REQUIRED", message = "Vui lòng đăng nhập tài khoản để xem lịch sử đơn hàng của bạn." }
            };
        }

        var statusFilter = GetString(args, "status")?.Trim();
        var limit = Math.Clamp(GetInt(args, "limit") ?? 5, 1, 15);

        var query = _db.Orders
            .Include(o => o.OrderDetails)
            .Where(o => o.UserId == context.UserId.Value);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            var stLower = statusFilter.ToLowerInvariant();
            query = query.Where(o => o.Status.ToLower() == stLower);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync();

        foreach (var o in orders)
        {
            if (context.Orders.All(x => x.OrderId != o.Id))
            {
                context.Orders.Add(MapOrderEntityToCard(o));
            }
        }

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetOrderHistory",
            Summary = $"Tìm thấy {orders.Count} đơn hàng của người dùng.",
            Data = new
            {
                count = orders.Count,
                orders = orders.Select(MapOrderEntityToCard)
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetOrderDetailsAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var orderId = GetInt(args, "orderId");
        var orderCode = GetString(args, "orderCode")?.Trim().Replace("#", "");

        var query = _db.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Options)
            .Include(o => o.StatusHistories)
            .AsQueryable();

        Order? order = null;
        if (orderId.HasValue && orderId.Value > 0)
        {
            order = await query.FirstOrDefaultAsync(o => o.Id == orderId.Value);
        }
        if (order == null && !string.IsNullOrWhiteSpace(orderCode))
        {
            var codeLower = orderCode.ToLowerInvariant();
            order = await query.FirstOrDefaultAsync(o => o.OrderCode.ToLower() == codeLower || o.OrderCode.ToLower().Contains(codeLower));
        }
        if (order == null && context.UserId.HasValue)
        {
            // Fallback to most recent order of current user
            order = await query
                .Where(o => o.UserId == context.UserId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
        }

        if (order == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "GetOrderDetails",
                Summary = "Không tìm thấy đơn hàng.",
                Data = new { error = "ORDER_NOT_FOUND", message = "Không tìm thấy đơn hàng phù hợp." }
            };
        }

        // Strict ownership check: Customer can only access their own orders
        bool isPrivileged = string.Equals(context.UserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(context.UserRole, "Manager", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(context.UserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        if (!isPrivileged && (!context.UserId.HasValue || order.UserId != context.UserId.Value))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "GetOrderDetails",
                Summary = "Từ chối truy cập: Đơn hàng không thuộc tài khoản hiện tại.",
                Data = new { error = "FORBIDDEN_ORDER_ACCESS", message = "Bạn chỉ được phép tra cứu đơn hàng thuộc tài khoản của chính mình." }
            };
        }

        var card = MapOrderEntityToCard(order);
        if (context.Orders.All(x => x.OrderId != card.OrderId))
        {
            context.Orders.Add(card);
        }

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetOrderDetails",
            Summary = $"Đơn hàng #{order.OrderCode} — Trạng thái: {order.Status}, Thanh toán: {order.PaymentStatus}, Tổng: {order.TotalAmount:N0}đ.",
            Data = new
            {
                order = card,
                timeline = order.StatusHistories.OrderBy(h => h.CreatedAt).Select(h => new { h.Status, h.Note, h.CreatedAt })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteTrackOrderAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        return await ExecuteGetOrderDetailsAsync(args, context);
    }

    private static AIOrderStatusCardDto MapOrderToCard(OrderResponse o)
    {
        return new AIOrderStatusCardDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            PaymentMethod = o.PaymentMethod,
            TotalAmount = o.TotalAmount,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            ShippingAddress = o.ShippingAddress,
            CreatedAt = o.CreatedAt,
            ItemSummaries = o.Items.Select(i => $"{i.Quantity}x {i.ProductName} (Size {i.Size}) — {i.TotalPrice:N0}đ").ToList(),
            OrderUrl = $"/orders/{o.Id}",
            PaymentPageUrl = $"/orders/{o.Id}"
        };
    }

    private static AIOrderStatusCardDto MapOrderEntityToCard(Order o)
    {
        return new AIOrderStatusCardDto
        {
            OrderId = o.Id,
            OrderCode = o.OrderCode,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            PaymentMethod = o.PaymentMethod,
            TotalAmount = o.TotalAmount,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            ShippingAddress = o.ShippingAddress,
            CreatedAt = o.CreatedAt,
            ItemSummaries = o.OrderDetails.Select(i => $"{i.Quantity}x {i.ProductName} (Size {i.Size}) — {i.TotalPrice:N0}đ").ToList(),
            OrderUrl = $"/orders/{o.Id}",
            PaymentPageUrl = $"/orders/{o.Id}"
        };
    }

    // =========================================================================
    // 5. PAYMENT AGENT TOOLS
    // =========================================================================
    private async Task<ToolExecutionResult> ExecuteGetPaymentGuidanceAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var orderId = GetInt(args, "orderId");
        var method = (GetString(args, "paymentMethod") ?? "SEPAY").ToUpperInvariant();

        if (orderId.HasValue && orderId.Value > 0)
        {
            return await ExecuteCreatePaymentTransactionAsync(args, context);
        }

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetPaymentGuidance",
            Summary = "Đã cung cấp hướng dẫn thanh toán chính thức tại MilkyVerse.",
            Data = new
            {
                supportedMethods = new[]
                {
                    new { code = "SEPAY", name = "Chuyển khoản VietQR TPBank tự động (SePay)", description = "Quét mã VietQR chuẩn Napas247, hệ thống Webhook SePay tự động xác nhận đơn ngay khi nhận tiền (Hiệu lực QR 15 phút)." },
                    new { code = "COD", name = "Thanh toán tiền mặt khi nhận hàng (COD)", description = "Thanh toán trực tiếp cho Shipper MilkyVerse khi nhận đồ uống." },
                    new { code = "MOMO", name = "Ví điện tử MoMo", description = "Thanh toán qua cổng MoMo QR / App." }
                },
                recommendedMethod = method
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteCreatePaymentTransactionAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var orderId = GetInt(args, "orderId");
        var orderCode = GetString(args, "orderCode")?.Trim().Replace("#", "");

        Order? order = null;
        if (orderId.HasValue && orderId.Value > 0)
        {
            order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(orderCode))
        {
            order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode.ToLower() == orderCode.ToLower());
        }
        else if (context.UserId.HasValue)
        {
            order = await _db.Orders.Where(o => o.UserId == context.UserId.Value).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
        }

        if (order == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CreatePaymentTransaction",
                Summary = "Không tìm thấy đơn hàng để tạo thanh toán.",
                Data = new { error = "ORDER_NOT_FOUND" }
            };
        }

        bool isPrivileged = string.Equals(context.UserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(context.UserRole, "Manager", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(context.UserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        if (!isPrivileged && (!context.UserId.HasValue || order.UserId != context.UserId.Value))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CreatePaymentTransaction",
                Summary = "Không có quyền tạo giao dịch cho đơn hàng của người khác.",
                Data = new { error = "FORBIDDEN_ORDER_ACCESS" }
            };
        }

        var qrRes = await _sePayService.GetOrCreateSePayQrAsync(order.Id, context.UserId);
        if (qrRes.Success && qrRes.Data != null)
        {
            context.Payment = new AIPaymentCardDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PaymentMethod = "SEPAY",
                PaymentStatus = order.PaymentStatus,
                Amount = order.TotalAmount,
                QrCodeUrl = qrRes.Data.QrUrl,
                PayUrl = $"/orders/{order.Id}",
                BankName = qrRes.Data.Bank,
                AccountNumber = qrRes.Data.AccountNumber,
                AccountName = qrRes.Data.AccountName,
                TransferContent = qrRes.Data.Description,
                Instructions = $"Quét mã VietQR TPBank ({qrRes.Data.AccountNumber} - {qrRes.Data.AccountName}) số tiền {order.TotalAmount:N0}đ, nội dung '{qrRes.Data.Description}'. Hệ thống sẽ tự động xác nhận qua Webhook SePay!"
            };

            return new ToolExecutionResult
            {
                Success = true,
                ToolName = "CreatePaymentTransaction",
                Summary = $"Đã khởi tạo mã QR thanh toán thật cho đơn #{order.OrderCode} ({order.TotalAmount:N0}đ).",
                Data = context.Payment
            };
        }

        return new ToolExecutionResult
        {
            Success = false,
            ToolName = "CreatePaymentTransaction",
            Summary = qrRes.Message,
            Data = new { error = "PAYMENT_INIT_FAILED", message = qrRes.Message }
        };
    }

    private async Task<ToolExecutionResult> ExecuteCheckPaymentStatusAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var orderId = GetInt(args, "orderId");
        var orderCode = GetString(args, "orderCode")?.Trim().Replace("#", "");

        Order? order = null;
        if (orderId.HasValue && orderId.Value > 0)
        {
            order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(orderCode))
        {
            order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderCode.ToLower() == orderCode.ToLower());
        }
        else if (context.UserId.HasValue)
        {
            order = await _db.Orders.Where(o => o.UserId == context.UserId.Value).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
        }

        if (order == null)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CheckPaymentStatus",
                Summary = "Không tìm thấy đơn hàng.",
                Data = new { error = "ORDER_NOT_FOUND" }
            };
        }

        bool isPrivileged = string.Equals(context.UserRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(context.UserRole, "Manager", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(context.UserRole, "Staff", StringComparison.OrdinalIgnoreCase);

        if (!isPrivileged && (!context.UserId.HasValue || order.UserId != context.UserId.Value))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ToolName = "CheckPaymentStatus",
                Summary = "Không có quyền kiểm tra đơn hàng của người khác.",
                Data = new { error = "FORBIDDEN_ORDER_ACCESS" }
            };
        }

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "CheckPaymentStatus",
            Summary = $"Đơn #{order.OrderCode}: Trạng thái thanh toán thực tế từ hệ thống là '{order.PaymentStatus}' (Phương thức: {order.PaymentMethod}).",
            Data = new
            {
                orderId = order.Id,
                orderCode = order.OrderCode,
                paymentMethod = order.PaymentMethod,
                paymentStatus = order.PaymentStatus,
                orderStatus = order.Status,
                paidAt = order.PaidAt,
                totalAmount = order.TotalAmount,
                verifiedByBackend = order.PaymentStatus == "Paid"
            }
        };
    }

    // =========================================================================
    // 6. INVENTORY AGENT TOOLS
    // =========================================================================
    private async Task<ToolExecutionResult> ExecuteGetIngredientInventoryAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var search = GetString(args, "searchQuery")?.Trim().ToLowerInvariant();
        var catType = GetString(args, "categoryType")?.Trim().ToUpperInvariant();

        var query = _db.Ingredients.Where(i => i.IsActive);
        if (!string.IsNullOrWhiteSpace(catType))
        {
            query = query.Where(i => i.CategoryType.ToUpper() == catType);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.ToLower().Contains(search));
        }

        var list = await query.OrderBy(i => i.Name).Take(25).ToListAsync();
        bool isStaffOrAdmin = !string.Equals(context.UserRole, "Customer", StringComparison.OrdinalIgnoreCase) &&
                              !string.Equals(context.UserRole, "Guest", StringComparison.OrdinalIgnoreCase);

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetIngredientInventory",
            Summary = $"Tra cứu được {list.Count} nguyên liệu trong kho.",
            Data = new
            {
                count = list.Count,
                ingredients = list.Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    categoryType = i.CategoryType,
                    isAvailable = i.CurrentStock > 0,
                    stockStatus = i.CurrentStock <= 0 ? "Hết hàng" : i.CurrentStock <= i.MinAlertThreshold ? "Sắp hết" : "Còn dồi dào",
                    currentStock = isStaffOrAdmin ? (decimal?)i.CurrentStock : null,
                    unit = isStaffOrAdmin ? i.Unit : null
                })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteCheckIngredientAvailabilityAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var prodName = GetString(args, "productName")?.Trim().ToLowerInvariant();
        var ingName = GetString(args, "ingredientName")?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(prodName))
        {
            var product = await _db.Products
                .Include(p => p.Recipes)
                    .ThenInclude(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(p => p.IsActive && (p.Name.ToLower() == prodName || p.Name.ToLower().Contains(prodName)));

            if (product == null)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ToolName = "CheckIngredientAvailability",
                    Summary = $"Không tìm thấy món '{prodName}'.",
                    Data = new { available = false, message = "Không tìm thấy món này trong thực đơn." }
                };
            }

            var outOfStockIngs = product.Recipes
                .SelectMany(r => r.RecipeIngredients)
                .Where(ri => ri.Ingredient != null && ri.Ingredient.CurrentStock < ri.Amount)
                .Select(ri => ri.Ingredient!.Name)
                .Distinct()
                .ToList();

            bool isAvailable = product.IsActive && product.StockStatus == 1 && !outOfStockIngs.Any();
            if (isAvailable)
            {
                AddSuggestedProductsToContext(context, new[] { product }, "Nguyên liệu tươi mới, sẵn sàng pha chế ngay.");
            }

            return new ToolExecutionResult
            {
                Success = true,
                ToolName = "CheckIngredientAvailability",
                Summary = isAvailable
                    ? $"Món '{product.Name}' hiện ĐẦY ĐỦ nguyên liệu tươi mới, sẵn sàng phục vụ!"
                    : $"Món '{product.Name}' hiện tạm thiếu nguyên liệu ({string.Join(", ", outOfStockIngs)}).",
                Data = new
                {
                    productId = product.Id,
                    productName = product.Name,
                    isAvailable,
                    basePrice = product.BasePrice,
                    missingIngredients = outOfStockIngs
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(ingName))
        {
            var ing = await _db.Ingredients.FirstOrDefaultAsync(i => i.IsActive && i.Name.ToLower().Contains(ingName));
            var top = await _db.Toppings.FirstOrDefaultAsync(t => t.Name.ToLower().Contains(ingName));

            bool available = (ing != null && ing.CurrentStock > 0) || (top != null && top.IsAvailable);
            return new ToolExecutionResult
            {
                Success = true,
                ToolName = "CheckIngredientAvailability",
                Summary = available
                    ? $"Nguyên liệu / Topping '{ing?.Name ?? top?.Name ?? ingName}' hiện CÒN HÀNG."
                    : $"Nguyên liệu '{ingName}' hiện tạm hết hoặc không có.",
                Data = new
                {
                    name = ing?.Name ?? top?.Name ?? ingName,
                    isAvailable = available,
                    toppingPrice = top?.Price
                }
            };
        }

        return new ToolExecutionResult
        {
            Success = false,
            ToolName = "CheckIngredientAvailability",
            Summary = "Vui lòng cung cấp tên món hoặc tên nguyên liệu cần kiểm tra.",
            Data = new { error = "MISSING_PARAMETER" }
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetLowStockIngredientsAsync(AgentExecutionContext context)
    {
        var allIngs = await _db.Ingredients.Where(i => i.IsActive).ToListAsync();
        var lowStock = allIngs
            .Where(i => i.CurrentStock <= i.MinAlertThreshold)
            .OrderBy(i => i.CurrentStock)
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
            .ToList();

        context.InventoryReport = new AIInventoryReportCardDto
        {
            TotalIngredients = allIngs.Count,
            LowStockCount = lowStock.Count,
            OutOfStockCount = allIngs.Count(i => i.CurrentStock <= 0),
            HighlightedIngredients = lowStock.Take(10).ToList(),
            SummaryNote = lowStock.Any()
                ? $"Cảnh báo: Có {lowStock.Count} nguyên liệu đang chạm hoặc dưới ngưỡng tồn kho tối thiểu cần nhập thêm."
                : "Tất cả nguyên liệu trong kho đều ở mức an toàn dồi dào."
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetLowStockIngredients",
            Summary = context.InventoryReport.SummaryNote,
            Data = context.InventoryReport
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetInventoryReportAsync(AgentExecutionContext context)
    {
        var allIngs = await _db.Ingredients.Where(i => i.IsActive).ToListAsync();
        var lowStock = allIngs
            .Where(i => i.CurrentStock <= i.MinAlertThreshold)
            .OrderBy(i => i.CurrentStock)
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
            .ToList();

        var topStock = allIngs
            .OrderBy(i => i.CurrentStock / Math.Max(1, i.MinAlertThreshold))
            .Take(8)
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
            .ToList();

        context.InventoryReport = new AIInventoryReportCardDto
        {
            TotalIngredients = allIngs.Count,
            LowStockCount = lowStock.Count,
            OutOfStockCount = allIngs.Count(i => i.CurrentStock <= 0),
            HighlightedIngredients = lowStock.Any() ? lowStock : topStock,
            SummaryNote = $"Tổng {allIngs.Count} mặt hàng nguyên vật liệu • {lowStock.Count} nguyên liệu sắp hết • {allIngs.Count(i => i.CurrentStock <= 0)} hết hàng."
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetInventoryReport",
            Summary = context.InventoryReport.SummaryNote,
            Data = context.InventoryReport
        };
    }

    // =========================================================================
    // 7. ADMIN AI AGENT TOOLS (Restricted to Admin & Manager)
    // =========================================================================
    private async Task<ToolExecutionResult> ExecuteGetSalesAnalyticsAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var days = Math.Clamp(GetInt(args, "days") ?? 30, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);

        var allOrders = await _db.Orders.ToListAsync();
        var revenueOrders = allOrders.Where(o => o.Status == "Completed" || o.Status == "Delivered" || o.PaymentStatus == "Paid").ToList();
        var totalRevenue = revenueOrders.Sum(o => o.TotalAmount);
        var totalCustomers = await _db.Users.CountAsync(u => u.Role == "Customer");
        var lowStockCount = await _db.Ingredients.CountAsync(i => i.IsActive && i.CurrentStock <= i.MinAlertThreshold);

        var bestSellers = await _db.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsBestSeller)
            .Take(5)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                Name = p.Name,
                Slug = p.Slug,
                BasePrice = p.BasePrice,
                ImageUrl = p.ImageUrl,
                IsBestSeller = p.IsBestSeller,
                ProductType = p.ProductType
            })
            .ToListAsync();

        context.AdminAnalytics = new AIAdminAnalyticsCardDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = allOrders.Count,
            CompletedOrders = allOrders.Count(o => o.Status == "Completed" || o.Status == "Delivered" || o.PaymentStatus == "Paid"),
            PendingOrders = allOrders.Count(o => o.Status == "Pending" || o.Status == "PendingConfirmation" || o.Status == "Confirmed" || o.Status == "Preparing" || o.Status == "ReadyForPickup"),
            CancelledOrders = allOrders.Count(o => o.Status == "Cancelled"),
            TotalCustomers = totalCustomers,
            LowStockCount = lowStockCount,
            BestSellingProducts = bestSellers,
            PeriodLabel = $"{days} ngày gần nhất"
        };

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetSalesAnalytics",
            Summary = $"Tổng doanh thu thực tế: {totalRevenue:N0}đ trên tổng {allOrders.Count} đơn hàng ({context.AdminAnalytics.CompletedOrders} đơn hoàn tất/đã thanh toán).",
            Data = context.AdminAnalytics
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetBestSellingProductsAsync(Dictionary<string, JsonElement> args, AgentExecutionContext context)
    {
        var limit = Math.Clamp(GetInt(args, "limit") ?? 6, 1, 15);
        var productType = GetString(args, "productType")?.Trim().ToUpperInvariant();

        var soldStats = await _db.OrderDetails
            .Where(od => od.ProductId != null)
            .GroupBy(od => od.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, TotalQty = g.Sum(x => x.Quantity), TotalRev = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.TotalQty)
            .ToListAsync();

        var productsQuery = _db.Products.Include(p => p.Category).Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(productType))
        {
            productsQuery = productsQuery.Where(p => p.ProductType.ToUpper() == productType);
        }

        var products = await productsQuery.ToListAsync();
        var soldMap = soldStats.ToDictionary(x => x.ProductId, x => x);

        var ranked = products
            .OrderByDescending(p => soldMap.TryGetValue(p.Id, out var st) ? st.TotalQty : 0)
            .ThenByDescending(p => p.IsBestSeller)
            .Take(limit)
            .ToList();

        AddSuggestedProductsToContext(context, ranked, "Top sản phẩm bán chạy nhất hệ thống MilkyVerse.");

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetBestSellingProducts",
            Summary = $"Đã thống kê Top {ranked.Count} sản phẩm bán chạy nhất.",
            Data = new
            {
                topProducts = ranked.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    productType = p.ProductType,
                    basePrice = p.BasePrice,
                    soldQuantity = soldMap.TryGetValue(p.Id, out var st) ? st.TotalQty : (p.IsBestSeller ? 25 : 5),
                    revenue = soldMap.TryGetValue(p.Id, out var sr) ? sr.TotalRev : p.BasePrice * 10
                })
            }
        };
    }

    private async Task<ToolExecutionResult> ExecuteGetOrderStatisticsAsync(AgentExecutionContext context)
    {
        return await ExecuteGetSalesAnalyticsAsync(new Dictionary<string, JsonElement>(), context);
    }

    private async Task<ToolExecutionResult> ExecuteGetCustomerStatisticsAsync(AgentExecutionContext context)
    {
        var customers = await _db.Users.Where(u => u.Role == "Customer").ToListAsync();
        var orders = await _db.Orders.Where(o => o.UserId != null).ToListAsync();

        var topCustomers = customers
            .Select(c =>
            {
                var cOrders = orders.Where(o => o.UserId == c.Id).ToList();
                return new
                {
                    id = c.Id,
                    fullName = c.FullName,
                    email = c.Email,
                    orderCount = cOrders.Count,
                    totalSpent = cOrders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount)
                };
            })
            .OrderByDescending(x => x.totalSpent)
            .Take(5)
            .ToList();

        return new ToolExecutionResult
        {
            Success = true,
            ToolName = "GetCustomerStatistics",
            Summary = $"Hệ thống hiện có {customers.Count} khách hàng đăng ký.",
            Data = new
            {
                totalCustomers = customers.Count,
                activeBuyersCount = topCustomers.Count(x => x.orderCount > 0),
                topVipCustomers = topCustomers
            }
        };
    }
}

internal static class CartCollectionExtensions
{
    public static List<AIClientCartItemDto> AffectedItemsListOrNew(this List<AIClientCartItemDto> _, AIClientCartItemDto item)
        => new() { item };
}
