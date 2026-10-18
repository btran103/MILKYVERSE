using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Data;

public class MilkyVerseDbContext : DbContext
{
    public MilkyVerseDbContext(DbContextOptions<MilkyVerseDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptItem> PurchaseReceiptItems => Set<PurchaseReceiptItem>();
    public DbSet<StockAudit> StockAudits => Set<StockAudit>();
    public DbSet<StockAuditItem> StockAuditItems => Set<StockAuditItem>();
    public DbSet<Topping> Toppings => Set<Topping>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderItemOption> OrderItemOptions => Set<OrderItemOption>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<DeliveryAssignment> DeliveryAssignments => Set<DeliveryAssignment>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Product indexes
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Slug)
            .IsUnique();

        // Order code index
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderCode)
            .IsUnique();

        // Promotion code index
        modelBuilder.Entity<Promotion>()
            .HasIndex(p => p.Code)
            .IsUnique();

        // Relationships
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComboItem>()
            .HasOne(ci => ci.ComboProduct)
            .WithMany(p => p.ComboItems)
            .HasForeignKey(ci => ci.ComboProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComboItem>()
            .HasOne(ci => ci.IncludedProduct)
            .WithMany()
            .HasForeignKey(ci => ci.IncludedProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.Product)
            .WithMany(p => p.Recipes)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeIngredients)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasOne(b => b.Ingredient)
            .WithMany(i => i.Batches)
            .HasForeignKey(b => b.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseReceipt>()
            .HasOne(pr => pr.CreatedByUser)
            .WithMany()
            .HasForeignKey(pr => pr.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseReceiptItem>()
            .HasOne(pri => pri.PurchaseReceipt)
            .WithMany(pr => pr.Items)
            .HasForeignKey(pri => pri.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseReceiptItem>()
            .HasOne(pri => pri.Ingredient)
            .WithMany()
            .HasForeignKey(pri => pri.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAudit>()
            .HasOne(sa => sa.AuditedByUser)
            .WithMany()
            .HasForeignKey(sa => sa.AuditedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockAuditItem>()
            .HasOne(sai => sai.StockAudit)
            .WithMany(sa => sa.Items)
            .HasForeignKey(sai => sai.StockAuditId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockAuditItem>()
            .HasOne(sai => sai.Ingredient)
            .WithMany()
            .HasForeignKey(sai => sai.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItemOption>()
            .HasOne(oio => oio.OrderDetail)
            .WithMany(od => od.Options)
            .HasForeignKey(oio => oio.OrderDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(h => h.Order)
            .WithMany(o => o.StatusHistories)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeliveryAssignment>()
            .HasOne(da => da.Order)
            .WithOne(o => o.DeliveryAssignment)
            .HasForeignKey<DeliveryAssignment>(da => da.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeliveryAssignment>()
            .HasOne(da => da.Shipper)
            .WithMany(u => u.DeliveryAssignments)
            .HasForeignKey(da => da.ShipperId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
