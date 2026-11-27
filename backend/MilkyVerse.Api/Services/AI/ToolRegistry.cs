using System.Text.Json;

namespace MilkyVerse.Api.Services.AI;

public class AIToolParameterSchema
{
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public List<string>? EnumValues { get; set; }
    public string? ItemsType { get; set; }
}

public class AIToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public HashSet<string> AllowedRoles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AIToolParameterSchema> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> RequiredParameters { get; set; } = new();
}

public interface IToolRegistry
{
    IReadOnlyList<AIToolDefinition> GetAllTools();
    IReadOnlyList<AIToolDefinition> GetToolsForRole(string? role);
    bool TryGetTool(string toolName, out AIToolDefinition? tool);
    bool IsRoleAuthorized(string toolName, string? role);
    object BuildGeminiToolsDeclaration(string? role);
    List<object> BuildOpenAiToolsDeclaration(string? role);
}

public class ToolRegistry : IToolRegistry
{
    private static readonly string[] AllRoles = { "Guest", "Customer", "Staff", "WarehouseStaff", "DeliveryStaff", "Manager", "Admin" };
    private static readonly string[] OrderingRoles = { "Guest", "Customer", "Staff", "Admin", "Manager" };
    private static readonly string[] StaffAndAboveRoles = { "Staff", "WarehouseStaff", "Manager", "Admin" };
    private static readonly string[] AdminAndManagerRoles = { "Admin", "Manager" };

    private readonly Dictionary<string, AIToolDefinition> _tools = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry()
    {
        RegisterAllTools();
    }

    private void Register(AIToolDefinition def)
    {
        _tools[def.Name] = def;
    }

    private void RegisterAllTools()
    {
        // =====================================================================
        // 0. CORE STAGE-2 IAgentTool DEFINITIONS (8 modular DI tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "search_menu",
            Category = "Product",
            Description = "Tìm kiếm thực đơn MilkyVerse (chỉ món IsActive). Lọc theo query, maxPrice, productType, noCaffeine, vegan, maxSpicyLevel, limit.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["query"] = new() { Type = "string", Description = "Từ khóa tên món hoặc hương vị" },
                ["minPrice"] = new() { Type = "number", Description = "Giá tối thiểu (VNĐ)" },
                ["maxPrice"] = new() { Type = "number", Description = "Giá tối đa / Ngân sách (VNĐ)" },
                ["productType"] = new() { Type = "string", Description = "DRINK, FOOD, hoặc COMBO", EnumValues = new() { "DRINK", "FOOD", "COMBO" } },
                ["noCaffeine"] = new() { Type = "boolean", Description = "True nếu tránh caffeine" },
                ["vegan"] = new() { Type = "boolean", Description = "True nếu thuần chay / không sữa động vật" },
                ["maxSpicyLevel"] = new() { Type = "integer", Description = "Độ cay tối đa (0..7)" },
                ["limit"] = new() { Type = "integer", Description = "Số món tối đa (1..12)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "get_product_detail",
            Category = "Product",
            Description = "Lấy thông tin chi tiết của một sản phẩm theo productId.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productId"] = new() { Type = "integer", Description = "ID sản phẩm trong database" }
            },
            RequiredParameters = new() { "productId" }
        });

        Register(new AIToolDefinition
        {
            Name = "list_toppings",
            Category = "DIY",
            Description = "Lấy danh sách các loại topping đang phục vụ kèm ID và giá.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        Register(new AIToolDefinition
        {
            Name = "quote_diy_drink",
            Category = "DIY",
            Description = "Tính giá công thức trà sữa tự pha DIY qua DIYService.QuoteDrinkAsync và phát sinh client action OPEN_DIY.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["teaBaseId"] = new() { Type = "string", Description = "black_tea, green_tea, oolong_tea, matcha_base", EnumValues = new() { "black_tea", "green_tea", "oolong_tea", "matcha_base" } },
                ["milkId"] = new() { Type = "string", Description = "fresh_milk, condensed_milk, oat_milk, almond_milk", EnumValues = new() { "fresh_milk", "condensed_milk", "oat_milk", "almond_milk" } },
                ["size"] = new() { Type = "string", Description = "M hoặc L", EnumValues = new() { "M", "L" } },
                ["sugar"] = new() { Type = "string", Description = "0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
                ["ice"] = new() { Type = "string", Description = "0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
                ["toppingIds"] = new() { Type = "array", ItemsType = "integer", Description = "Danh sách ID topping" }
            },
            RequiredParameters = new() { "teaBaseId", "milkId", "size", "sugar", "ice" }
        });

        Register(new AIToolDefinition
        {
            Name = "validate_coupon",
            Category = "Shopping",
            Description = "Kiểm tra mã giảm giá theo tạm tính đơn hàng (subtotal) và phát sinh client action APPLY_COUPON.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["code"] = new() { Type = "string", Description = "Mã giảm giá (VD: MILKYNEW, FREESHIP, AIBARISTA)" },
                ["subtotal"] = new() { Type = "number", Description = "Tổng tiền tạm tính (VNĐ)" }
            },
            RequiredParameters = new() { "code", "subtotal" }
        });

        Register(new AIToolDefinition
        {
            Name = "get_my_orders",
            Category = "Order",
            Description = "Lấy danh sách đơn hàng của chính người dùng đang đăng nhập.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["limit"] = new() { Type = "integer", Description = "Số lượng đơn tối đa (1..15)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "get_order_status",
            Category = "Order",
            Description = "Tra cứu trạng thái đơn hàng theo orderCode (chỉ trả đơn thuộc chính userId đang đăng nhập).",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["orderCode"] = new() { Type = "string", Description = "Mã đơn hàng (VD: MV2026...)" }
            },
            RequiredParameters = new() { "orderCode" }
        });

        Register(new AIToolDefinition
        {
            Name = "add_to_cart",
            Category = "Shopping",
            Description = "Validate tham số phía server (productId tồn tại, quantity 1..20, sugar/ice hợp lệ) và trả về client action ADD_TO_CART cho Frontend.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productId"] = new() { Type = "integer", Description = "ID sản phẩm trong database" },
                ["quantity"] = new() { Type = "integer", Description = "Số lượng từ 1 đến 20" },
                ["size"] = new() { Type = "string", Description = "M, L, hoặc Standard", EnumValues = new() { "M", "L", "Standard" } },
                ["sugar"] = new() { Type = "string", Description = "0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
                ["ice"] = new() { Type = "string", Description = "0%, 30%, 50%, 70%, 100%", EnumValues = new() { "0%", "30%", "50%", "70%", "100%" } },
                ["toppingIds"] = new() { Type = "array", ItemsType = "integer", Description = "Danh sách ID topping" }
            },
            RequiredParameters = new() { "productId" }
        });

        // =====================================================================
        // 1. PRODUCT AGENT TOOLS (5 tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "SearchProducts",
            Category = "Product",
            Description = "Tìm kiếm đồ uống, đồ ăn vặt hoặc combo trong thực đơn MilkyVerse theo từ khóa tên món, danh mục hoặc nguyên liệu.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["query"] = new() { Type = "string", Description = "Từ khóa tên món, hương vị hoặc nguyên liệu (VD: matcha, trân châu, khoai môn, đào, cá viên, mì cay)" },
                ["category"] = new() { Type = "string", Description = "Tên hoặc slug danh mục (VD: tra-sua, tra-trai-cay, ca-phe, an-vat, combo)" },
                ["productType"] = new() { Type = "string", Description = "Loại sản phẩm: DRINK, FOOD, hoặc COMBO", EnumValues = new() { "DRINK", "FOOD", "COMBO" } },
                ["limit"] = new() { Type = "integer", Description = "Số lượng kết quả tối đa (mặc định 6)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "GetProductDetails",
            Category = "Product",
            Description = "Lấy thông tin chi tiết của một sản phẩm cụ thể (giá bán, mô tả, thành phần, tùy chọn size, đường, đá, độ cay, sốt chấm, trạng thái còn hàng).",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productId"] = new() { Type = "integer", Description = "ID sản phẩm trong database" },
                ["productName"] = new() { Type = "string", Description = "Tên sản phẩm cần xem chi tiết" },
                ["slug"] = new() { Type = "string", Description = "Slug đường dẫn của sản phẩm" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "RecommendDrinks",
            Category = "Product",
            Description = "Đề xuất đồ uống thông minh theo tâm trạng, nhu cầu sức khỏe (đau họng, mất ngủ, tỉnh táo chạy deadline, giảm cân), khẩu vị và ngân sách.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["moodOrNeed"] = new() { Type = "string", Description = "Nhu cầu hoặc tâm trạng khách hàng (VD: đau họng, buổi tối sợ mất ngủ, buồn ngủ chạy deadline, ăn kiêng, giải nhiệt, béo ngậy)" },
                ["maxBudget"] = new() { Type = "number", Description = "Ngân sách tối đa (VNĐ)" },
                ["sugarPreference"] = new() { Type = "string", Description = "Mức đường mong muốn (0%, 30%, 50%, 70%, 100%)" },
                ["avoidCaffeine"] = new() { Type = "boolean", Description = "True nếu khách muốn tránh caffeine" }
            },
            RequiredParameters = new() { "moodOrNeed" }
        });

        Register(new AIToolDefinition
        {
            Name = "FilterProductsByPrice",
            Category = "Product",
            Description = "Lọc danh sách đồ uống và món ăn theo khoảng giá hoặc ngân sách tối đa của khách hàng.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["minPrice"] = new() { Type = "number", Description = "Giá tối thiểu (VNĐ)" },
                ["maxPrice"] = new() { Type = "number", Description = "Ngân sách / Giá tối đa (VNĐ, VD: 40000)" },
                ["productType"] = new() { Type = "string", Description = "Loại món: DRINK, FOOD, COMBO" },
                ["keyword"] = new() { Type = "string", Description = "Từ khóa kèm theo (VD: trà sữa, trà trái cây)" },
                ["limit"] = new() { Type = "integer", Description = "Số món tối đa (mặc định 6)" }
            },
            RequiredParameters = new() { "maxPrice" }
        });

        Register(new AIToolDefinition
        {
            Name = "FilterProductsByTaste",
            Category = "Product",
            Description = "Lọc đồ uống hoặc đồ ăn theo hương vị, độ ngọt, độ béo, không sữa, không caffeine hoặc cấp độ cay.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["tasteProfile"] = new() { Type = "string", Description = "Hương vị mong muốn (VD: ít ngọt, thanh mát, chua ngọt, béo ngậy, đậm vị trà, cay nồng)" },
                ["sweetnessLevel"] = new() { Type = "string", Description = "Mức đường gợi ý (0%, 30%, 50%, 70%, 100%)" },
                ["noCaffeine"] = new() { Type = "boolean", Description = "True nếu chọn món không caffeine" },
                ["noMilk"] = new() { Type = "boolean", Description = "True nếu chọn món không sữa động vật" },
                ["hasBoba"] = new() { Type = "boolean", Description = "True nếu khách thích có trân châu" },
                ["maxPrice"] = new() { Type = "number", Description = "Giới hạn giá tối đa nếu có (VNĐ)" }
            },
            RequiredParameters = new() { "tasteProfile" }
        });

        // =====================================================================
        // 2. DIY DRINK STUDIO AGENT TOOLS (6 tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "GenerateRecipe",
            Category = "DIY",
            Description = "Sáng tạo công thức pha chế trà sữa. Phân biệt rõ giữa công thức tham khảo sáng tạo và công thức DIY thực tế có thể đặt mua tại quán.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["themeOrCraving"] = new() { Type = "string", Description = "Chủ đề hoặc sở thích hương vị khách muốn sáng tạo (VD: matcha yến mạch ít ngọt, ô long kem cheese hoàng kim)" },
                ["teaBaseId"] = new() { Type = "string", Description = "Mã cốt trà DIY: black_tea, green_tea, oolong_tea, matcha_base" },
                ["milkId"] = new() { Type = "string", Description = "Mã loại sữa DIY: fresh_milk, condensed_milk, oat_milk, almond_milk" },
                ["size"] = new() { Type = "string", Description = "Kích cỡ: M hoặc L", EnumValues = new() { "M", "L" } },
                ["sugarLevel"] = new() { Type = "string", Description = "Độ ngọt: 0%, 30%, 50%, 70%, 100%" },
                ["iceLevel"] = new() { Type = "string", Description = "Lượng đá: 0%, 30%, 50%, 70%, 100%" },
                ["toppingNames"] = new() { Type = "array", ItemsType = "string", Description = "Danh sách tên topping kết hợp" },
                ["isReferenceOnly"] = new() { Type = "boolean", Description = "True nếu là công thức tham khảo tại nhà, False nếu là cấu hình DIY đặt hàng tại MilkyVerse" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "CustomizeDrink",
            Category = "DIY",
            Description = "Tùy chỉnh cấu hình một ly đồ uống có sẵn trong menu hoặc ly tự pha DIY (size, loại trà, sữa, độ ngọt, đá, topping) và kiểm tra tính hợp lệ.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productId"] = new() { Type = "integer", Description = "ID sản phẩm trong menu (bỏ trống nếu là ly DIY tự phối)" },
                ["productName"] = new() { Type = "string", Description = "Tên sản phẩm trong menu (hoặc tên ly DIY)" },
                ["isCustomDIY"] = new() { Type = "boolean", Description = "True nếu thiết kế qua DIY Drink Studio" },
                ["teaBaseId"] = new() { Type = "string", Description = "Mã cốt trà DIY: black_tea, green_tea, oolong_tea, matcha_base" },
                ["milkId"] = new() { Type = "string", Description = "Mã sữa DIY: fresh_milk, condensed_milk, oat_milk, almond_milk" },
                ["size"] = new() { Type = "string", Description = "Kích cỡ: M hoặc L" },
                ["sugarLevel"] = new() { Type = "string", Description = "Độ ngọt: 0%, 30%, 50%, 70%, 100%" },
                ["iceLevel"] = new() { Type = "string", Description = "Mức đá: 0%, 30%, 50%, 70%, 100%" },
                ["toppingNames"] = new() { Type = "array", ItemsType = "string", Description = "Danh sách tên các topping muốn thêm" },
                ["quantity"] = new() { Type = "integer", Description = "Số lượng ly (mặc định 1)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "CalculateDrinkPrice",
            Category = "DIY",
            Description = "Tính giá chính xác của ly đồ uống hoặc cấu hình DIY dựa trên quy tắc giá của backend MilkyVerse.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productId"] = new() { Type = "integer", Description = "ID sản phẩm (nếu là món trong thực đơn)" },
                ["productName"] = new() { Type = "string", Description = "Tên sản phẩm (nếu là món trong thực đơn)" },
                ["isCustomDIY"] = new() { Type = "boolean", Description = "True nếu tính giá công thức DIY" },
                ["teaBaseId"] = new() { Type = "string", Description = "black_tea, green_tea, oolong_tea, matcha_base" },
                ["milkId"] = new() { Type = "string", Description = "fresh_milk, condensed_milk, oat_milk, almond_milk" },
                ["size"] = new() { Type = "string", Description = "M hoặc L" },
                ["toppingNames"] = new() { Type = "array", ItemsType = "string", Description = "Danh sách tên hoặc ID topping" },
                ["quantity"] = new() { Type = "integer", Description = "Số lượng ly" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "GetAvailableToppings",
            Category = "DIY",
            Description = "Lấy danh sách toàn bộ các loại Topping đang có sẵn tại MilkyVerse kèm giá tiền thực tế.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        Register(new AIToolDefinition
        {
            Name = "GetAvailableIngredients",
            Category = "DIY",
            Description = "Lấy danh sách nguyên liệu pha chế (Cốt trà, Sữa, Đường, Đá, Topping của DIY Drink Studio và nguyên liệu hiện có).",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["categoryType"] = new() { Type = "string", Description = "Loại nguyên liệu: DIY, BEVERAGE, FOOD" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "SaveFavoriteRecipe",
            Category = "DIY",
            Description = "Lưu công thức đồ uống DIY yêu thích vào bộ nhớ tài khoản của khách hàng để dễ dàng gọi lại lần sau.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["recipeName"] = new() { Type = "string", Description = "Tên đặt cho công thức yêu thích" },
                ["teaBaseId"] = new() { Type = "string", Description = "black_tea, green_tea, oolong_tea, matcha_base" },
                ["milkId"] = new() { Type = "string", Description = "fresh_milk, condensed_milk, oat_milk, almond_milk" },
                ["size"] = new() { Type = "string", Description = "M hoặc L" },
                ["sugarLevel"] = new() { Type = "string", Description = "0%, 30%, 50%, 70%, 100%" },
                ["iceLevel"] = new() { Type = "string", Description = "0%, 30%, 50%, 70%, 100%" },
                ["toppingNames"] = new() { Type = "array", ItemsType = "string", Description = "Danh sách topping" },
                ["notes"] = new() { Type = "string", Description = "Ghi chú hương vị" }
            },
            RequiredParameters = new() { "recipeName" }
        });

        // =====================================================================
        // 3. SHOPPING & CART AGENT TOOLS (5 tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "GetCart",
            Category = "Shopping",
            Description = "Xem danh sách các món hiện có trong giỏ hàng của khách hàng kèm đơn giá và tổng tiền.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        Register(new AIToolDefinition
        {
            Name = "AddToCart",
            Category = "Shopping",
            Description = "Thêm một đồ uống, món ăn vặt hoặc ly tự pha DIY vào giỏ hàng của khách với cấu hình (size, đường, đá, topping, số lượng) và giá chuẩn từ backend.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productId"] = new() { Type = "integer", Description = "ID sản phẩm trong thực đơn" },
                ["productName"] = new() { Type = "string", Description = "Tên sản phẩm muốn thêm vào giỏ" },
                ["isCustomDIY"] = new() { Type = "boolean", Description = "True nếu là ly DIY tự phối" },
                ["teaBaseId"] = new() { Type = "string", Description = "Mã cốt trà nếu là DIY" },
                ["milkId"] = new() { Type = "string", Description = "Mã sữa nếu là DIY" },
                ["size"] = new() { Type = "string", Description = "Kích cỡ: M, L, hoặc Standard" },
                ["sugarLevel"] = new() { Type = "string", Description = "Mức đường: 0%, 30%, 50%, 70%, 100%" },
                ["iceLevel"] = new() { Type = "string", Description = "Mức đá: 0%, 30%, 50%, 70%, 100%" },
                ["spicyLevel"] = new() { Type = "string", Description = "Cấp độ cay cho đồ ăn (VD: Không cay, Cấp 1, Cấp 3)" },
                ["dippingSauce"] = new() { Type = "string", Description = "Sốt chấm cho đồ ăn vặt" },
                ["toppingNames"] = new() { Type = "array", ItemsType = "string", Description = "Danh sách tên các topping thêm vào ly" },
                ["quantity"] = new() { Type = "integer", Description = "Số lượng (mặc định 1)" },
                ["customNotes"] = new() { Type = "string", Description = "Ghi chú thêm cho pha chế" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "UpdateCartItem",
            Category = "Shopping",
            Description = "Cập nhật số lượng hoặc cấu hình (size, đường, đá) của một món đã có trong giỏ hàng.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["cartItemId"] = new() { Type = "string", Description = "ID của dòng giỏ hàng" },
                ["productName"] = new() { Type = "string", Description = "Tên món trong giỏ cần cập nhật" },
                ["newQuantity"] = new() { Type = "integer", Description = "Số lượng mới (nếu bằng 0 sẽ xóa món)" },
                ["size"] = new() { Type = "string", Description = "Size mới nếu muốn đổi (M hoặc L)" },
                ["sugarLevel"] = new() { Type = "string", Description = "Mức đường mới" },
                ["iceLevel"] = new() { Type = "string", Description = "Mức đá mới" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "RemoveCartItem",
            Category = "Shopping",
            Description = "Xóa một món khỏi giỏ hàng theo ID hoặc tên món.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["cartItemId"] = new() { Type = "string", Description = "ID của món trong giỏ" },
                ["productName"] = new() { Type = "string", Description = "Tên món cần xóa khỏi giỏ" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "ClearCart",
            Category = "Shopping",
            Description = "Xóa toàn bộ các món đang có trong giỏ hàng hiện tại.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        // =====================================================================
        // 4. ORDER AGENT TOOLS (4 tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "CreateOrder",
            Category = "Order",
            Description = "Tạo đơn hàng thật từ giỏ hàng hiện tại sau khi khách hàng ĐÃ XÁC NHẬN thông tin nhận hàng (Họ tên, SĐT, Địa chỉ) và phương thức thanh toán.",
            AllowedRoles = new HashSet<string>(OrderingRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["customerName"] = new() { Type = "string", Description = "Họ tên người nhận hàng" },
                ["customerPhone"] = new() { Type = "string", Description = "Số điện thoại người nhận" },
                ["shippingAddress"] = new() { Type = "string", Description = "Địa chỉ giao hàng chi tiết" },
                ["paymentMethod"] = new() { Type = "string", Description = "Phương thức thanh toán: COD, SEPAY, MOMO", EnumValues = new() { "COD", "SEPAY", "MOMO" } },
                ["promotionCode"] = new() { Type = "string", Description = "Mã giảm giá nếu khách cung cấp (VD: MILKYNEW, FREESHIP, AIBARISTA)" },
                ["note"] = new() { Type = "string", Description = "Ghi chú giao hàng" },
                ["confirmedByCustomer"] = new() { Type = "boolean", Description = "Bắt buộc phải là true khi khách đã xác nhận đồng ý đặt đơn" }
            },
            RequiredParameters = new() { "customerName", "customerPhone", "shippingAddress", "confirmedByCustomer" }
        });

        Register(new AIToolDefinition
        {
            Name = "GetOrderHistory",
            Category = "Order",
            Description = "Lấy lịch sử danh sách các đơn hàng của người dùng hiện tại.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["status"] = new() { Type = "string", Description = "Lọc theo trạng thái đơn: Pending, Confirmed, Preparing, Delivering, Completed, Cancelled" },
                ["limit"] = new() { Type = "integer", Description = "Số đơn tối đa cần lấy (mặc định 5)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "GetOrderDetails",
            Category = "Order",
            Description = "Xem thông tin chi tiết của một đơn hàng thuộc người dùng hiện tại (danh sách món, topping, tổng tiền, trạng thái thanh toán).",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["orderId"] = new() { Type = "integer", Description = "ID số của đơn hàng" },
                ["orderCode"] = new() { Type = "string", Description = "Mã đơn hàng (VD: MV2026...)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "TrackOrder",
            Category = "Order",
            Description = "Theo dõi tiến trình xử lý và vận chuyển thời gian thực của đơn hàng (Chờ xác nhận -> Đã xác nhận -> Đang pha chế -> Đang giao -> Hoàn tất).",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["orderId"] = new() { Type = "integer", Description = "ID đơn hàng" },
                ["orderCode"] = new() { Type = "string", Description = "Mã đơn hàng" }
            }
        });

        // =====================================================================
        // 5. PAYMENT AGENT TOOLS (3 tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "GetPaymentGuidance",
            Category = "Payment",
            Description = "Tư vấn các phương thức thanh toán (SePay VietQR TPBank tự động, MoMo, COD) và tạo thông tin/mã QR thanh toán thật từ backend cho đơn hàng.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["paymentMethod"] = new() { Type = "string", Description = "Phương thức: SEPAY, MOMO, COD" },
                ["orderId"] = new() { Type = "integer", Description = "ID đơn hàng nếu cần lấy mã QR hoặc link thanh toán thật" },
                ["orderCode"] = new() { Type = "string", Description = "Mã đơn hàng nếu có" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "CreatePaymentTransaction",
            Category = "Payment",
            Description = "Yêu cầu backend khởi tạo phiên thanh toán VietQR SePay hoặc MoMo cho đơn hàng và trả về URL/QR thanh toán thật.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["orderId"] = new() { Type = "integer", Description = "ID đơn hàng cần thanh toán" },
                ["paymentMethod"] = new() { Type = "string", Description = "SEPAY hoặc MOMO" }
            },
            RequiredParameters = new() { "orderId" }
        });

        Register(new AIToolDefinition
        {
            Name = "CheckPaymentStatus",
            Category = "Payment",
            Description = "Kiểm tra kết quả giao dịch thanh toán thực tế của đơn hàng từ cơ sở dữ liệu backend.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["orderId"] = new() { Type = "integer", Description = "ID đơn hàng" },
                ["orderCode"] = new() { Type = "string", Description = "Mã đơn hàng" }
            }
        });

        // =====================================================================
        // 6. INVENTORY AGENT TOOLS (4 tools)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "GetIngredientInventory",
            Category = "Inventory",
            Description = "Tra cứu tồn kho nguyên liệu pha chế & chế biến.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["searchQuery"] = new() { Type = "string", Description = "Tên nguyên liệu cần tìm (VD: trân châu, trà, sữa, phô mai)" },
                ["categoryType"] = new() { Type = "string", Description = "Nhóm nguyên liệu: BEVERAGE, FOOD, PACKAGING" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "CheckIngredientAvailability",
            Category = "Inventory",
            Description = "Kiểm tra xem một món ăn/đồ uống hoặc nguyên liệu cụ thể còn hàng và đủ định lượng pha chế trong kho hay không.",
            AllowedRoles = new HashSet<string>(AllRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["productName"] = new() { Type = "string", Description = "Tên sản phẩm cần kiểm tra nguyên liệu" },
                ["ingredientName"] = new() { Type = "string", Description = "Tên nguyên liệu hoặc topping cần kiểm tra" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "GetLowStockIngredients",
            Category = "Inventory",
            Description = "Lấy danh sách các nguyên liệu trong kho đang dưới ngưỡng cảnh báo tồn kho tối thiểu (Dành cho Staff, Thủ kho, Quản lý và Admin).",
            AllowedRoles = new HashSet<string>(StaffAndAboveRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        Register(new AIToolDefinition
        {
            Name = "GetInventoryReport",
            Category = "Inventory",
            Description = "Báo cáo tổng hợp tình hình kho nguyên liệu, lô hàng sắp hết hạn (FEFO) và giá trị tồn kho (Dành cho Thủ kho, Quản lý và Admin).",
            AllowedRoles = new HashSet<string>(StaffAndAboveRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        // =====================================================================
        // 7. ADMIN AI AGENT TOOLS (4 tools - Restricted to Admin & Manager)
        // =====================================================================
        Register(new AIToolDefinition
        {
            Name = "GetSalesAnalytics",
            Category = "Admin",
            Description = "Phân tích doanh thu thực tế, tổng giá trị đơn hàng đã thanh toán/hoàn tất và hiệu suất kinh doanh (Chỉ dành cho Admin/Manager).",
            AllowedRoles = new HashSet<string>(AdminAndManagerRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["days"] = new() { Type = "integer", Description = "Số ngày gần đây cần phân tích (mặc định 30)" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "GetBestSellingProducts",
            Category = "Admin",
            Description = "Thống kê danh sách các sản phẩm bán chạy nhất theo số lượng bán thực tế và doanh thu (Chỉ dành cho Admin/Manager).",
            AllowedRoles = new HashSet<string>(AdminAndManagerRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
            {
                ["limit"] = new() { Type = "integer", Description = "Số lượng sản phẩm top đầu (mặc định 6)" },
                ["productType"] = new() { Type = "string", Description = "Lọc theo DRINK, FOOD, COMBO" }
            }
        });

        Register(new AIToolDefinition
        {
            Name = "GetOrderStatistics",
            Category = "Admin",
            Description = "Thống kê số lượng đơn hàng theo từng trạng thái (Pending, Confirmed, Preparing, Delivering, Completed, Cancelled) và tỷ lệ thanh toán (Chỉ dành cho Admin/Manager).",
            AllowedRoles = new HashSet<string>(AdminAndManagerRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });

        Register(new AIToolDefinition
        {
            Name = "GetCustomerStatistics",
            Category = "Admin",
            Description = "Thống kê tổng số khách hàng, khách hàng mua nhiều nhất (Top VIP) và tần suất đặt hàng (Chỉ dành cho Admin/Manager).",
            AllowedRoles = new HashSet<string>(AdminAndManagerRoles, StringComparer.OrdinalIgnoreCase),
            Parameters = new()
        });
    }

    public IReadOnlyList<AIToolDefinition> GetAllTools() => _tools.Values.ToList();

    public IReadOnlyList<AIToolDefinition> GetToolsForRole(string? role)
    {
        var effectiveRole = string.IsNullOrWhiteSpace(role) ? "Guest" : role.Trim();
        return _tools.Values
            .Where(t => t.AllowedRoles.Contains(effectiveRole))
            .ToList();
    }

    public bool TryGetTool(string toolName, out AIToolDefinition? tool)
    {
        return _tools.TryGetValue(toolName, out tool);
    }

    public bool IsRoleAuthorized(string toolName, string? role)
    {
        if (!_tools.TryGetValue(toolName, out var tool)) return false;
        var effectiveRole = string.IsNullOrWhiteSpace(role) ? "Guest" : role.Trim();
        return tool.AllowedRoles.Contains(effectiveRole);
    }

    public object BuildGeminiToolsDeclaration(string? role)
    {
        var allowedTools = GetToolsForRole(role);
        var declarations = allowedTools.Select(t =>
        {
            var props = new Dictionary<string, object>();
            foreach (var kv in t.Parameters)
            {
                var propObj = new Dictionary<string, object>
                {
                    ["type"] = kv.Value.Type.ToUpperInvariant(),
                    ["description"] = kv.Value.Description
                };
                if (kv.Value.EnumValues != null && kv.Value.EnumValues.Any())
                {
                    propObj["enum"] = kv.Value.EnumValues;
                }
                if (kv.Value.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
                {
                    propObj["items"] = new { type = (kv.Value.ItemsType ?? "STRING").ToUpperInvariant() };
                }
                props[kv.Key] = propObj;
            }

            return new
            {
                name = t.Name,
                description = t.Description,
                parameters = new
                {
                    type = "OBJECT",
                    properties = props,
                    required = t.RequiredParameters
                }
            };
        }).ToList();

        return new[]
        {
            new { functionDeclarations = declarations }
        };
    }

    public List<object> BuildOpenAiToolsDeclaration(string? role)
    {
        var allowedTools = GetToolsForRole(role);
        return allowedTools.Select(t =>
        {
            var props = new Dictionary<string, object>();
            foreach (var kv in t.Parameters)
            {
                var propObj = new Dictionary<string, object>
                {
                    ["type"] = kv.Value.Type.ToLowerInvariant(),
                    ["description"] = kv.Value.Description
                };
                if (kv.Value.EnumValues != null && kv.Value.EnumValues.Any())
                {
                    propObj["enum"] = kv.Value.EnumValues;
                }
                if (kv.Value.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
                {
                    propObj["items"] = new { type = (kv.Value.ItemsType ?? "string").ToLowerInvariant() };
                }
                props[kv.Key] = propObj;
            }

            return (object)new
            {
                type = "function",
                function = new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = new
                    {
                        type = "object",
                        properties = props,
                        required = t.RequiredParameters
                    }
                }
            };
        }).ToList();
    }
}
