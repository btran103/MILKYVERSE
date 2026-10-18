namespace MilkyVerse.Api.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "") => new() { Success = true, Data = data, Message = message };
    public static ApiResponse<T> Fail(string message) => new() { Success = false, Message = message, Data = default };
}

// ==========================================
// 1. AUTH & USER MANAGEMENT DTOs
// ==========================================
public class SendOtpRequest
{
    public string Email { get; set; } = string.Empty;
}

public class SendOtpResultDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool SentViaSmtp { get; set; }
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? VerificationCode { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff"; // Manager, Staff, WarehouseStaff, DeliveryStaff, Customer
}

public class UpdateUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

// ==========================================
// 2. PRODUCT, CATEGORY & FOOD OPTIONS DTOs
// ==========================================
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string CategoryType { get; set; } = "DRINK"; // DRINK, FOOD, COMBO
    public int ProductCount { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsBestSeller { get; set; }
    public bool IsNew { get; set; }
    public bool IsActive { get; set; }
    public int StockStatus { get; set; }
    public string ProductType { get; set; } = "DRINK"; // DRINK, FOOD, COMBO
    public int? SpicyLevel { get; set; }
    public bool AllowSpicyCustomization { get; set; }
    public bool AllowSauceCustomization { get; set; }
    public string DefaultSauce { get; set; } = string.Empty;
    public string PortionDescription { get; set; } = string.Empty;
}

public class ToppingDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public class ProductDetailDto : ProductDto
{
    public List<ToppingDto> AvailableToppings { get; set; } = new();
    public List<string> Sizes { get; set; } = new() { "M", "L" };
    public List<string> SugarLevels { get; set; } = new() { "0%", "30%", "50%", "70%", "100%" };
    public List<string> IceLevels { get; set; } = new() { "0%", "30%", "50%", "70%", "100%" };
    public List<string> SpicyOptions { get; set; } = new() { "Cấp 0 - Không cay", "Cấp 1 - Cay nhẹ", "Cấp 2 - Cay vừa", "Cấp 3 - Cay nồng", "Cấp 5 - Rất cay", "Cấp 7 - Siêu cay" };
    public List<string> SauceOptions { get; set; } = new() { "Tương ớt cay nồng", "Sốt me chua ngọt đặc chế", "Sốt bơ trứng béo ngậy", "Muối tiêu chanh ớt xanh", "Sốt phô mai cay" };
    public List<string> PortionOptions { get; set; } = new() { "Phần tiêu chuẩn", "Phần lớn (+15.000đ)", "Phần gia đình / khổng lồ (+35.000đ)" };
    public List<ProductDto> RelatedProducts { get; set; } = new();
}

public class CreateProductRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsBestSeller { get; set; }
    public bool IsNew { get; set; }
    public int StockStatus { get; set; } = 1;
    public string ProductType { get; set; } = "DRINK";
    public int? SpicyLevel { get; set; }
    public bool AllowSpicyCustomization { get; set; }
    public bool AllowSauceCustomization { get; set; }
    public string DefaultSauce { get; set; } = string.Empty;
    public string PortionDescription { get; set; } = string.Empty;
}

public class UpdateProductRequest : CreateProductRequest
{
    public bool IsActive { get; set; } = true;
}

// ==========================================
// 3. DIY DRINK STUDIO DTOs
// ==========================================
public class DIYOptionItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal AdditionalPrice { get; set; }
    public string ColorHex { get; set; } = "#C5A059";
    public bool IsAvailable { get; set; } = true;
}

public class DIYCatalogDto
{
    public List<DIYOptionItem> TeaBases { get; set; } = new();
    public List<DIYOptionItem> Milks { get; set; } = new();
    public List<DIYOptionItem> Sizes { get; set; } = new();
    public List<DIYOptionItem> SweetnessLevels { get; set; } = new();
    public List<DIYOptionItem> IceLevels { get; set; } = new();
    public List<ToppingDto> Toppings { get; set; } = new();
}

public class DIYQuoteRequest
{
    public string TeaBaseId { get; set; } = string.Empty;
    public string MilkId { get; set; } = string.Empty;
    public string Size { get; set; } = "M";
    public string SugarLevel { get; set; } = "100%";
    public string IceLevel { get; set; } = "100%";
    public List<int> ToppingIds { get; set; } = new();
    public int Quantity { get; set; } = 1;
}

public class DIYQuoteResponse
{
    public bool IsValid { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public List<string> Breakdown { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}

// ==========================================
// 4. CART & ORDER DTOs
// ==========================================
public class OrderItemToppingRequest
{
    public int ToppingId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CreateOrderItemRequest
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ItemType { get; set; } = "DRINK"; // DRINK, FOOD, COMBO
    public string Size { get; set; } = "M";
    public string SugarLevel { get; set; } = "100%";
    public string IceLevel { get; set; } = "100%";
    public string SpicyLevel { get; set; } = "Không cay";
    public string DippingSauce { get; set; } = "Tương ớt";
    public string PortionSize { get; set; } = "Tiêu chuẩn";
    public int Quantity { get; set; } = 1;
    public bool IsCustomDIY { get; set; } = false;
    public string CustomNotes { get; set; } = string.Empty;
    public List<OrderItemToppingRequest> Toppings { get; set; } = new();
}

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public string? PromotionCode { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class OrderItemOptionResponse
{
    public int ToppingId { get; set; }
    public string ToppingName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderDetailResponse
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ItemType { get; set; } = "DRINK";
    public string Size { get; set; } = "M";
    public string SugarLevel { get; set; } = "100%";
    public string IceLevel { get; set; } = "100%";
    public string SpicyLevel { get; set; } = string.Empty;
    public string DippingSauce { get; set; } = string.Empty;
    public string PortionSize { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsCustomDIY { get; set; }
    public string CustomNotes { get; set; } = string.Empty;
    public List<OrderItemOptionResponse> Toppings { get; set; } = new();
}

public class OrderResponse
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderDetailResponse> Items { get; set; } = new();

    // Delivery info
    public int? ShipperId { get; set; }
    public string ShipperName { get; set; } = string.Empty;
    public string ShipperPhone { get; set; } = string.Empty;
    public string DeliveryStatus { get; set; } = string.Empty;
    public decimal CodAmount { get; set; } = 0;
    public bool IsCodCollected { get; set; } = false;
    public bool CodReconciled { get; set; } = false;
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    public string Note { get; set; } = string.Empty;
}

// ==========================================
// 5. WAREHOUSE & INVENTORY DTOs (FEFO)
// ==========================================
public class IngredientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinAlertThreshold { get; set; }
    public decimal UnitCost { get; set; }
    public string CategoryType { get; set; } = "BEVERAGE";
    public bool IsLowStock => CurrentStock <= MinAlertThreshold;
}

public class StockBatchDto
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ManufactureDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal InitialQuantity { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public int DaysRemaining => (int)(ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays;
    public bool IsExpired => DaysRemaining <= 0;
    public bool IsNearExpiry => DaysRemaining > 0 && DaysRemaining <= 15;
}

public class PurchaseReceiptItemDto
{
    public int IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(6);
}

public class CreatePurchaseReceiptRequest
{
    public string SupplierName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseReceiptItemDto> Items { get; set; } = new();
}

public class PurchaseReceiptResponseDto
{
    public int Id { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class StockAuditItemRequestDto
{
    public int IngredientId { get; set; }
    public decimal ActualQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CreateStockAuditRequest
{
    public string Notes { get; set; } = string.Empty;
    public List<StockAuditItemRequestDto> Items { get; set; } = new();
}

public class StockAuditItemResponseDto
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SystemQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal Variance { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class StockAuditResponseDto
{
    public int Id { get; set; }
    public string AuditCode { get; set; } = string.Empty;
    public DateTime AuditDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<StockAuditItemResponseDto> Items { get; set; } = new();
}

public class StockAdjustRequest
{
    public int IngredientId { get; set; }
    public string TransactionType { get; set; } = "IMPORT"; // IMPORT, EXPORT, ADJUST
    public decimal Quantity { get; set; }
    public string Note { get; set; } = string.Empty;
}

// ==========================================
// 6. DELIVERY & SHIPPER DTOs
// ==========================================
public class AssignShipperRequest
{
    public int OrderId { get; set; }
    public int ShipperId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class UpdateDeliveryStatusRequest
{
    public string Status { get; set; } = "Delivered"; // PickedUp, InTransit, Delivered, Failed
    public string FailureReason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsCodCollected { get; set; } = false;
    public string ProofImageUrl { get; set; } = string.Empty;
}

public class ShipperDashboardDto
{
    public int TotalAssignedOrders { get; set; }
    public int InTransitOrders { get; set; }
    public int DeliveredToday { get; set; }
    public decimal TotalCodCollected { get; set; }
    public decimal TotalCodPendingReconcile { get; set; }
    public List<OrderResponse> Orders { get; set; } = new();
}

public class ReconcileCodRequest
{
    public List<int> OrderIds { get; set; } = new();
}

// ==========================================
// 7. PROMOTION DTOs
// ==========================================
public class PromotionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MinOrderValue { get; set; }
    public bool IsActive { get; set; }
}

public class ValidatePromoRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OrderSubtotal { get; set; }
}

public class ValidatePromoResponse
{
    public bool IsValid { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ==========================================
// 8. AI BARISTA DTOs
// ==========================================
public class SuggestedDrinkDto
{
    public int? ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedPrice { get; set; }
    public string SugarRecommend { get; set; } = "50%";
    public string IceRecommend { get; set; } = "70%";
    public List<string> RecommendedToppings { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

public class AIChatRequest
{
    public int? SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AIChatResponse
{
    public int SessionId { get; set; }
    public string Reply { get; set; } = string.Empty;
    public List<SuggestedDrinkDto> SuggestedDrinks { get; set; } = new();
    public bool IsAiModelLive { get; set; }
}

// ==========================================
// 9. ADMIN & MANAGER DASHBOARD DTOs
// ==========================================
public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int LowStockCount { get; set; }
    public int PendingOrdersCount { get; set; }
    public int DeliveringOrdersCount { get; set; }
    public List<OrderResponse> RecentOrders { get; set; } = new();
    public List<ProductDto> TopProducts { get; set; } = new();
    public List<IngredientDto> LowStockIngredients { get; set; } = new();
}
