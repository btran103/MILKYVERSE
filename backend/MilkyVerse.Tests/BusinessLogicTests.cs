using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;
using MilkyVerse.Api.Services;
using Xunit;

namespace MilkyVerse.Tests;

public class BusinessLogicTests
{
    private MilkyVerseDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<MilkyVerseDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new MilkyVerseDbContext(options);
    }

    [Fact]
    public async Task AuthService_RegisterAndLogin_Success()
    {
        using var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Jwt:Key", "MilkyVerseTestSecurityKey2026SuperSecretStringHere!" },
            { "Jwt:Issuer", "MilkyVerse" },
            { "Jwt:Audience", "MilkyVerseUsers" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var authService = new AuthService(context, config);

        // 1. Register
        var registerRes = await authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Nguyen Van Test",
            Email = "test@milkyverse.com",
            Password = "Password@123",
            PhoneNumber = "0900000000",
            Address = "HCM City"
        });

        Assert.True(registerRes.Success);
        Assert.NotEmpty(registerRes.Data!.Token);
        Assert.Equal("Customer", registerRes.Data.Role);

        // 2. Prevent duplicate email
        var duplicateRes = await authService.RegisterAsync(new RegisterRequest
        {
            FullName = "Nguyen Duplicate",
            Email = "test@milkyverse.com",
            Password = "Password@123"
        });
        Assert.False(duplicateRes.Success);

        // 3. Login with correct password
        var loginRes = await authService.LoginAsync(new LoginRequest
        {
            Email = "test@milkyverse.com",
            Password = "Password@123"
        });
        Assert.True(loginRes.Success);
        Assert.NotEmpty(loginRes.Data!.Token);

        // 4. Login with wrong password fails
        var failLogin = await authService.LoginAsync(new LoginRequest
        {
            Email = "test@milkyverse.com",
            Password = "WrongPassword"
        });
        Assert.False(failLogin.Success);
    }

    [Fact]
    public async Task DIYService_QuoteDrink_CalculatesPriceCorrectly()
    {
        using var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var topping = new Topping { Name = "Trân Châu Hoàng Kim", Price = 10000, IsAvailable = true };
        context.Toppings.Add(topping);
        await context.SaveChangesAsync();

        var diyService = new DIYService(context);

        // Black Tea (28k) + Fresh Milk (12k) + Size L (8k) + Topping (10k) = 58k
        var quoteRes = await diyService.QuoteDrinkAsync(new DIYQuoteRequest
        {
            TeaBaseId = "black_tea",
            MilkId = "fresh_milk",
            Size = "L",
            SugarLevel = "50%",
            IceLevel = "70%",
            ToppingIds = new List<int> { topping.Id },
            Quantity = 2
        });

        Assert.True(quoteRes.Success);
        Assert.Equal(58000, quoteRes.Data!.UnitPrice);
        Assert.Equal(116000, quoteRes.Data.TotalPrice);
        Assert.Equal(2, quoteRes.Data.Quantity);
    }

    [Fact]
    public async Task OrderService_ValidatePromotion_ChecksMinOrderAndPercent()
    {
        using var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var promo = new Promotion
        {
            Code = "MILKY20",
            Description = "Giảm 20% cho đơn từ 100k",
            DiscountPercent = 20,
            MinOrderValue = 100000,
            MaxUsage = 100,
            UsedCount = 0,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(10),
            IsActive = true
        };
        context.Promotions.Add(promo);
        await context.SaveChangesAsync();

        var orderService = new OrderService(context);

        // 1. Order below min value
        var belowRes = await orderService.ValidatePromotionAsync(new ValidatePromoRequest
        {
            Code = "MILKY20",
            OrderSubtotal = 80000
        });
        Assert.False(belowRes.Data!.IsValid);

        // 2. Order qualifying
        var validRes = await orderService.ValidatePromotionAsync(new ValidatePromoRequest
        {
            Code = "MILKY20",
            OrderSubtotal = 100000
        });
        Assert.True(validRes.Data!.IsValid);
        Assert.Equal(20000, validRes.Data.DiscountAmount);
    }

    [Fact]
    public async Task InventoryService_AdjustStock_UpdatesBalanceAndTransactions()
    {
        using var context = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var ingredient = new Ingredient
        {
            Name = "Trà Xanh Lài",
            Unit = "g",
            CurrentStock = 5000,
            MinAlertThreshold = 1000,
            UnitCost = 50,
            IsActive = true
        };
        context.Ingredients.Add(ingredient);
        await context.SaveChangesAsync();

        var inventoryService = new InventoryService(context);

        // Import 2000g
        var adjustRes = await inventoryService.AdjustStockAsync(new StockAdjustRequest
        {
            IngredientId = ingredient.Id,
            TransactionType = "IMPORT",
            Quantity = 2000,
            Note = "Nhập thêm kho"
        });

        Assert.True(adjustRes.Success);

        var updated = await context.Ingredients.FindAsync(ingredient.Id);
        Assert.Equal(7000, updated!.CurrentStock);

        var txs = await inventoryService.GetTransactionsAsync(ingredient.Id);
        Assert.NotEmpty(txs.Data!);
        Assert.Equal("IMPORT", txs.Data![0].TransactionType);
    }
}
