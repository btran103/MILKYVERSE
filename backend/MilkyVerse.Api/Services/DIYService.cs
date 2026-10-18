using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;

namespace MilkyVerse.Api.Services;

public interface IDIYService
{
    Task<ApiResponse<DIYCatalogDto>> GetCatalogAsync();
    Task<ApiResponse<DIYQuoteResponse>> QuoteDrinkAsync(DIYQuoteRequest request);
}

public class DIYService : IDIYService
{
    private readonly MilkyVerseDbContext _context;

    public DIYService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<DIYCatalogDto>> GetCatalogAsync()
    {
        var toppings = await _context.Toppings
            .Where(t => t.IsAvailable)
            .Select(t => new ToppingDto
            {
                Id = t.Id,
                Name = t.Name,
                Price = t.Price,
                IsAvailable = t.IsAvailable
            })
            .ToListAsync();

        var catalog = new DIYCatalogDto
        {
            TeaBases = new List<DIYOptionItem>
            {
                new() { Id = "black_tea", Name = "Hồng Trà Ceylon Thượng Hạng", Description = "Đậm đà nồng nàn, thơm hương hoa quả khô và hậu vị ngọt sâu", AdditionalPrice = 28000, ColorHex = "#8B4513" },
                new() { Id = "green_tea", Name = "Lục Trà Lài Thanh Khiết", Description = "Thơm ngát hương hoa nhài mùa hạ, thanh thoát và dịu nhẹ", AdditionalPrice = 28000, ColorHex = "#6B8E23" },
                new() { Id = "oolong_tea", Name = "Trà Ô Long Rang Khói Thảo Mộc", Description = "Hương thơm rang mộc đặc trưng từ than hồng, vị dày béo tròn vị", AdditionalPrice = 32000, ColorHex = "#A0522D" },
                new() { Id = "matcha_base", Name = "Matcha Uji Kyoto Nguyên Chất", Description = "100% bột matcha Nhật Bản thượng hạng, béo mộc tự nhiên", AdditionalPrice = 36000, ColorHex = "#2E8B57" }
            },
            Milks = new List<DIYOptionItem>
            {
                new() { Id = "fresh_milk", Name = "Sữa Tươi Thanh Trùng Dalat", Description = "Tươi mát nguyên chất, béo nhẹ thanh tao", AdditionalPrice = 12000, ColorHex = "#FFFDF0" },
                new() { Id = "condensed_milk", Name = "Sữa Đặc Kem Béo Truyền Thống", Description = "Vị ngọt béo đậm đà chuẩn gu trà sữa cổ điển", AdditionalPrice = 10000, ColorHex = "#FBF4D9" },
                new() { Id = "oat_milk", Name = "Sữa Yến Mạch Oatly (Thuần Chay)", Description = "Không lactose, tốt cho sức khỏe, thơm bùi ngậy nhẹ", AdditionalPrice = 18000, ColorHex = "#EADCC2" },
                new() { Id = "almond_milk", Name = "Sữa Hạnh Nhân Hữu Cơ", Description = "Thơm bùi tự nhiên, ít calo, giàu vitamin E", AdditionalPrice = 20000, ColorHex = "#EDE0D4" }
            },
            Sizes = new List<DIYOptionItem>
            {
                new() { Id = "M", Name = "Size Vừa (M - 500ml)", Description = "Kích thước tiêu chuẩn phù hợp thưởng thức đơn", AdditionalPrice = 0 },
                new() { Id = "L", Name = "Size Lớn (L - 700ml)", Description = "Uống đã hơn, tăng 40% thể tích cốt trà & sữa", AdditionalPrice = 8000 }
            },
            SweetnessLevels = new List<DIYOptionItem>
            {
                new() { Id = "0%", Name = "0% Đường (Không ngọt)", Description = "Giữ trọn vị nguyên bản đậm đà của trà và sữa" },
                new() { Id = "30%", Name = "30% Đường (Ít ngọt)", Description = "Ngọt thanh nhè nhẹ, phù hợp lối sống lành mạnh" },
                new() { Id = "50%", Name = "50% Đường (Vừa phải)", Description = "Cân bằng lý tưởng giữa vị trà, độ béo và vị ngọt" },
                new() { Id = "70%", Name = "70% Đường (Ngọt dịu)", Description = "Vị ngọt dịu dàng lan tỏa" },
                new() { Id = "100%", Name = "100% Đường (Chuẩn vị)", Description = "Độ ngọt tiêu chuẩn truyền thống đậm đà" }
            },
            IceLevels = new List<DIYOptionItem>
            {
                new() { Id = "0%", Name = "0% Đá (Không đá)", Description = "Giữ trọn ly trà không bị loãng" },
                new() { Id = "30%", Name = "30% Đá (Ít đá)", Description = "Mát nhẹ, vị trà sữa cô đọng" },
                new() { Id = "50%", Name = "50% Đá (Vừa)", Description = "Mát lạnh sảng khoái" },
                new() { Id = "70%", Name = "70% Đá", Description = "Lạnh sâu kích thích vị giác" },
                new() { Id = "100%", Name = "100% Đá (Đá chuẩn)", Description = "Mát lạnh cực đã xua tan nắng nóng" }
            },
            Toppings = toppings
        };

        return ApiResponse<DIYCatalogDto>.Ok(catalog);
    }

    public async Task<ApiResponse<DIYQuoteResponse>> QuoteDrinkAsync(DIYQuoteRequest request)
    {
        var catalogRes = await GetCatalogAsync();
        var catalog = catalogRes.Data!;

        var teaBase = catalog.TeaBases.FirstOrDefault(t => t.Id == request.TeaBaseId);
        if (teaBase == null)
        {
            return ApiResponse<DIYQuoteResponse>.Fail("Nền trà không hợp lệ.");
        }

        var milk = catalog.Milks.FirstOrDefault(m => m.Id == request.MilkId);
        if (milk == null)
        {
            return ApiResponse<DIYQuoteResponse>.Fail("Thành phần sữa/bổ sung không hợp lệ.");
        }

        var size = catalog.Sizes.FirstOrDefault(s => s.Id == request.Size);
        if (size == null)
        {
            return ApiResponse<DIYQuoteResponse>.Fail("Kích cỡ không hợp lệ.");
        }

        // Validate Toppings from DB
        decimal toppingsPrice = 0;
        var selectedToppingsNames = new List<string>();

        if (request.ToppingIds != null && request.ToppingIds.Any())
        {
            var validToppings = await _context.Toppings
                .Where(t => request.ToppingIds.Contains(t.Id) && t.IsAvailable)
                .ToListAsync();

            if (validToppings.Count != request.ToppingIds.Distinct().Count())
            {
                return ApiResponse<DIYQuoteResponse>.Fail("Một số topping bạn chọn hiện không khả dụng hoặc đã hết hàng.");
            }

            foreach (var top in validToppings)
            {
                toppingsPrice += top.Price;
                selectedToppingsNames.Add($"{top.Name} (+{top.Price:N0}đ)");
            }
        }

        decimal unitPrice = teaBase.AdditionalPrice + milk.AdditionalPrice + size.AdditionalPrice + toppingsPrice;
        int qty = Math.Max(1, request.Quantity);
        decimal totalPrice = unitPrice * qty;

        var breakdown = new List<string>
        {
            $"{teaBase.Name}: {teaBase.AdditionalPrice:N0}đ",
            $"{milk.Name}: +{milk.AdditionalPrice:N0}đ",
            $"Kích cỡ {size.Name}: +{size.AdditionalPrice:N0}đ"
        };
        breakdown.AddRange(selectedToppingsNames);

        var recipeName = $"Trà Sữa Tự Phối: {teaBase.Name} x {milk.Name}";

        var response = new DIYQuoteResponse
        {
            IsValid = true,
            RecipeName = recipeName,
            UnitPrice = unitPrice,
            Quantity = qty,
            TotalPrice = totalPrice,
            Breakdown = breakdown
        };

        return ApiResponse<DIYQuoteResponse>.Ok(response);
    }
}
