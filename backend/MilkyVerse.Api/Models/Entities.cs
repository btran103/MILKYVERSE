using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MilkyVerse.Api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Customer"; // Customer, Staff, Manager, WarehouseStaff, DeliveryStaff, Admin

    public bool IsActive { get; set; } = true;

    public bool IsEmailVerified { get; set; } = false;
    public string VerificationCode { get; set; } = string.Empty;
    public DateTime? VerificationCodeExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<DeliveryAssignment> DeliveryAssignments { get; set; } = new List<DeliveryAssignment>();
}

public class EmailVerification
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ImageUrl { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    [MaxLength(30)]
    public string CategoryType { get; set; } = "DRINK"; // DRINK, FOOD, COMBO

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    [Key]
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal BasePrice { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsBestSeller { get; set; } = false;
    public bool IsNew { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int StockStatus { get; set; } = 1; // 1: InStock, 0: OutOfStock

    [MaxLength(30)]
    public string ProductType { get; set; } = "DRINK"; // DRINK, FOOD, COMBO

    public int? SpicyLevel { get; set; } = 0; // 0: Không cay, 1: Cay nhẹ, 3: Cay vừa, 5: Cay nồng, 7: Siêu cay
    public bool AllowSpicyCustomization { get; set; } = false;
    public bool AllowSauceCustomization { get; set; } = false;

    [MaxLength(100)]
    public string DefaultSauce { get; set; } = "Tương ớt cay nồng";

    [MaxLength(100)]
    public string PortionDescription { get; set; } = "1 phần";

    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();
}

public class ComboItem
{
    [Key]
    public int Id { get; set; }

    public int ComboProductId { get; set; }
    public Product? ComboProduct { get; set; }

    public int IncludedProductId { get; set; }
    public Product? IncludedProduct { get; set; }

    public int Quantity { get; set; } = 1;

    [MaxLength(200)]
    public string CustomNotes { get; set; } = string.Empty;
}

public class Ingredient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "g"; // g, ml, portion, piece

    [Column(TypeName = "decimal(12,2)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal MinAlertThreshold { get; set; } = 100;

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitCost { get; set; } = 0;

    [MaxLength(50)]
    public string CategoryType { get; set; } = "BEVERAGE"; // BEVERAGE, FOOD, PACKAGING

    public bool IsActive { get; set; } = true;

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<StockBatch> Batches { get; set; } = new List<StockBatch>();
}

public class StockBatch
{
    [Key]
    public int Id { get; set; }

    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    [Required]
    [MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateTime ManufactureDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(6);

    [Column(TypeName = "decimal(12,2)")]
    public decimal InitialQuantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal CurrentQuantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PurchaseReceipt
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReceiptCode { get; set; } = string.Empty;

    [MaxLength(150)]
    public string SupplierName { get; set; } = string.Empty;

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalCost { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "Completed"; // Pending, Completed, Cancelled

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseReceiptItem> Items { get; set; } = new List<PurchaseReceiptItem>();
}

public class PurchaseReceiptItem
{
    [Key]
    public int Id { get; set; }

    public int PurchaseReceiptId { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }

    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(6);
}

public class StockAudit
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string AuditCode { get; set; } = string.Empty;

    public int? AuditedByUserId { get; set; }
    public User? AuditedByUser { get; set; }

    public DateTime AuditDate { get; set; } = DateTime.UtcNow;

    [MaxLength(30)]
    public string Status { get; set; } = "Completed"; // Draft, Completed

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public ICollection<StockAuditItem> Items { get; set; } = new List<StockAuditItem>();
}

public class StockAuditItem
{
    [Key]
    public int Id { get; set; }

    public int StockAuditId { get; set; }
    public StockAudit? StockAudit { get; set; }

    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal SystemQuantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal ActualQuantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Variance { get; set; }

    [MaxLength(255)]
    public string Reason { get; set; } = string.Empty;
}

public class Topping
{
    [Key]
    public int Id { get; set; }

    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal PortionAmount { get; set; } = 40; // in Ingredient Unit (e.g., 40g per portion)

    public bool IsAvailable { get; set; } = true;
}

public class Recipe
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    [MaxLength(30)]
    public string Size { get; set; } = "Standard"; // Standard, M, L, XL

    [MaxLength(255)]
    public string Notes { get; set; } = string.Empty;

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}

public class RecipeIngredient
{
    [Key]
    public int Id { get; set; }

    public int RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }
}

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderCode { get; set; } = string.Empty;

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal ShippingFee { get; set; } = 15000;

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Preparing, ReadyForPickup, Delivering, Completed, Cancelled

    [Required]
    [MaxLength(30)]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed

    [Required]
    [MaxLength(30)]
    public string PaymentMethod { get; set; } = "COD"; // COD, VIETQR, VNPAY, MOMO

    [Required]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ShippingAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
    public DeliveryAssignment? DeliveryAssignment { get; set; }
}

public class OrderDetail
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    [MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string ItemType { get; set; } = "DRINK"; // DRINK, FOOD, COMBO

    [Required]
    [MaxLength(30)]
    public string Size { get; set; } = "M"; // M, L, Standard

    [MaxLength(30)]
    public string SugarLevel { get; set; } = "100%"; // 0%, 30%, 50%, 70%, 100%

    [MaxLength(30)]
    public string IceLevel { get; set; } = "100%"; // 0%, 30%, 50%, 70%, 100%

    [MaxLength(50)]
    public string SpicyLevel { get; set; } = "Không cay";

    [MaxLength(100)]
    public string DippingSauce { get; set; } = "Tương ớt";

    [MaxLength(50)]
    public string PortionSize { get; set; } = "Tiêu chuẩn";

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalPrice { get; set; }

    public bool IsCustomDIY { get; set; } = false;

    [MaxLength(500)]
    public string CustomNotes { get; set; } = string.Empty;

    public ICollection<OrderItemOption> Options { get; set; } = new List<OrderItemOption>();
}

public class OrderItemOption
{
    [Key]
    public int Id { get; set; }

    public int OrderDetailId { get; set; }
    public OrderDetail? OrderDetail { get; set; }

    public int? ToppingId { get; set; }
    public Topping? Topping { get; set; }

    [Required]
    [MaxLength(100)]
    public string ToppingName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;
}

public class DeliveryAssignment
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ShipperId { get; set; }
    public User? Shipper { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Assigned"; // Assigned, PickedUp, InTransit, Delivered, Failed

    [Column(TypeName = "decimal(12,2)")]
    public decimal CodAmount { get; set; } = 0;

    public bool IsCodCollected { get; set; } = false;
    public bool CodReconciled { get; set; } = false;

    [MaxLength(255)]
    public string FailureReason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ProofImageUrl { get; set; } = string.Empty;
}

public class OrderStatusHistory
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Promotion
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercent { get; set; } = 0; // e.g. 15.00 for 15%

    [Column(TypeName = "decimal(12,2)")]
    public decimal DiscountAmount { get; set; } = 0; // Flat discount

    [Column(TypeName = "decimal(12,2)")]
    public decimal MinOrderValue { get; set; } = 0;

    public int MaxUsage { get; set; } = 1000;
    public int UsedCount { get; set; } = 0;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class InventoryTransaction
{
    [Key]
    public int Id { get; set; }

    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }

    [Required]
    [MaxLength(30)]
    public string TransactionType { get; set; } = "IMPORT"; // IMPORT, EXPORT, ADJUST, ORDER_DEDUCT

    [Column(TypeName = "decimal(12,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal BalanceAfter { get; set; }

    [MaxLength(50)]
    public string ReferenceType { get; set; } = string.Empty; // Order, Manual, Delivery, PurchaseReceipt, StockAudit

    [MaxLength(50)]
    public string ReferenceId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatSession
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [MaxLength(150)]
    public string SessionTitle { get; set; } = "Trò chuyện mới";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    public int SessionId { get; set; }
    public ChatSession? Session { get; set; }

    [Required]
    [MaxLength(20)]
    public string SenderRole { get; set; } = "User"; // User, Assistant

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? SuggestedDrinkJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
