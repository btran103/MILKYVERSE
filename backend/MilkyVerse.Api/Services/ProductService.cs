using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IProductService
{
    Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync();
    Task<ApiResponse<List<ProductDto>>> GetProductsAsync(int? categoryId = null, string? search = null, decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null, string? productType = null);
    Task<ApiResponse<ProductDetailDto>> GetProductBySlugAsync(string slug);
    Task<ApiResponse<ProductDetailDto>> GetProductByIdAsync(int id);
    Task<ApiResponse<List<ToppingDto>>> GetToppingsAsync();
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<ApiResponse<bool>> DeleteProductAsync(int id);
    Task<ApiResponse<List<ProductReviewDto>>> GetReviewsAsync(int productId);
    Task<ApiResponse<List<ProductReviewDto>>> GetAllRecentReviewsAsync(int limit = 10);
    Task<ApiResponse<ProductReviewDto>> AddReviewAsync(int productId, int userId, string userName, string? userAvatar, CreateProductReviewRequest request);
}

public class ProductService : IProductService
{
    private readonly MilkyVerseDbContext _context;

    public ProductService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                CategoryType = c.CategoryType,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        return ApiResponse<List<CategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<List<ProductDto>>> GetProductsAsync(
        int? categoryId = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? productType = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(productType))
        {
            query = query.Where(p => p.ProductType.ToUpper() == productType.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Description.ToLower().Contains(s));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= maxPrice.Value);
        }

        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "name" => query.OrderBy(p => p.Name),
            "newest" => query.OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Id),
            _ => query.OrderByDescending(p => p.IsBestSeller).ThenBy(p => p.Id)
        };

        var products = await query.Select(p => new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : string.Empty,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            BasePrice = p.BasePrice,
            ImageUrl = p.ImageUrl,
            ImagePlaceholder = p.ImagePlaceholder,
            IsBestSeller = p.IsBestSeller,
            IsNew = p.IsNew,
            IsActive = p.IsActive,
            StockStatus = p.StockStatus,
            ProductType = p.ProductType,
            SpicyLevel = p.SpicyLevel,
            AllowSpicyCustomization = p.AllowSpicyCustomization,
            AllowSauceCustomization = p.AllowSauceCustomization,
            DefaultSauce = p.DefaultSauce,
            PortionDescription = p.PortionDescription
        }).ToListAsync();

        return ApiResponse<List<ProductDto>>.Ok(products);
    }

    public async Task<ApiResponse<ProductDetailDto>> GetProductBySlugAsync(string slug)
    {
        var p = await _context.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);

        if (p == null)
        {
            return ApiResponse<ProductDetailDto>.Fail("Không tìm thấy món ăn / đồ uống này.");
        }

        return await BuildProductDetailDto(p);
    }

    public async Task<ApiResponse<ProductDetailDto>> GetProductByIdAsync(int id)
    {
        var p = await _context.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
        {
            return ApiResponse<ProductDetailDto>.Fail("Không tìm thấy sản phẩm.");
        }

        return await BuildProductDetailDto(p);
    }

    private async Task<ApiResponse<ProductDetailDto>> BuildProductDetailDto(Product p)
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

        var related = await _context.Products
            .Where(r => r.CategoryId == p.CategoryId && r.Id != p.Id && r.IsActive)
            .Take(4)
            .Select(r => new ProductDto
            {
                Id = r.Id,
                CategoryId = r.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "",
                Name = r.Name,
                Slug = r.Slug,
                Description = r.Description,
                BasePrice = r.BasePrice,
                ImageUrl = r.ImageUrl,
                ImagePlaceholder = r.ImagePlaceholder,
                IsBestSeller = r.IsBestSeller,
                IsNew = r.IsNew,
                IsActive = r.IsActive,
                StockStatus = r.StockStatus,
                ProductType = r.ProductType,
                SpicyLevel = r.SpicyLevel,
                AllowSpicyCustomization = r.AllowSpicyCustomization,
                AllowSauceCustomization = r.AllowSauceCustomization,
                DefaultSauce = r.DefaultSauce,
                PortionDescription = r.PortionDescription
            })
            .ToListAsync();

        var reviews = new List<ProductReviewDto>();
        try
        {
            reviews = await _context.ProductReviews
                .Where(r => r.ProductId == p.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.UserName,
                    UserAvatar = r.UserAvatar,
                    Rating = r.Rating,
                    Verdict = r.Verdict,
                    Comment = r.Comment,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();
        }
        catch
        {
            // Table may not exist yet or no reviews
        }

        double avgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;

        var comboItems = new List<ComboItemDto>();
        if (p.ProductType == "COMBO")
        {
            comboItems = await _context.ComboItems
                .Where(c => c.ComboProductId == p.Id)
                .Include(c => c.IncludedProduct)
                .Select(c => new ComboItemDto
                {
                    Id = c.Id,
                    IncludedProductId = c.IncludedProductId,
                    ProductName = c.IncludedProduct != null ? c.IncludedProduct.Name : "",
                    Slug = c.IncludedProduct != null ? c.IncludedProduct.Slug : "",
                    ImageUrl = c.IncludedProduct != null ? c.IncludedProduct.ImageUrl : "",
                    BasePrice = c.IncludedProduct != null ? c.IncludedProduct.BasePrice : 0,
                    ProductType = c.IncludedProduct != null ? c.IncludedProduct.ProductType : "DRINK",
                    Quantity = c.Quantity,
                    CustomNotes = c.CustomNotes
                })
                .ToListAsync();
        }

        // Auto-generate realistic ingredient list based on product type & name
        var ingredients = new List<string>();
        var nameLower = (p.Name ?? "").ToLower();
        var type = (p.ProductType ?? "DRINK").ToUpper();

        if (type == "FOOD")
        {
            if (nameLower.Contains("cá viên") || nameLower.Contains("chiên"))
            {
                ingredients.AddRange(new[] { "Thịt cá ba sa & tôm sú quết nhuyễn tươi sạch", "Gia vị tiêu sọ Đắk Lắk & hạt nêm tự nhiên", "Dầu thực vật dinh dưỡng chiên mới mỗi ca", "Dưa leo tươi & rau răm ăn kèm chuẩn VietGAP", "Sốt chấm tương đen xào sa tế gia truyền" });
            }
            else if (nameLower.Contains("bánh tráng"))
            {
                ingredients.AddRange(new[] { "Bánh tráng phơi sương Trảng Bàng Tây Ninh loại 1", "Khô bò sợi cay thượng hạng", "Khô gà lá chanh thơm lừng", "Xoài xanh non thái sợi giòn ngọt", "Trứng cút luộc lòng đào", "Nước sốt bò sa tế tắc đậm đà", "Hành phi giòn & đậu phộng rang muối" });
            }
            else if (nameLower.Contains("chân gà"))
            {
                ingredients.AddRange(new[] { "Chân gà rút xương sạch loại A giòn sần sật", "Sả cây & lá chanh Thái thơm nồng", "Cóc non bao tử & xoài giòn", "Nước sốt Thái chua cay mặn ngọt đặc chế từ ớt xiêm rừng", "Tắc tươi vắt lấy nước cốt thanh mát" });
            }
            else if (nameLower.Contains("mì cay") || nameLower.Contains("mi cay"))
            {
                ingredients.AddRange(new[] { "Mì sợi tròn Hàn Quốc nấu dai mềm chuẩn vị Koreno", "Tôm sú tươi & mực nang giòn ngọt", "Bò Mỹ cuộn nấm kim châm", "Bắp cải tím, bông cải xanh & nấm kim châm sạch", "Nước dùng xương hầm 8 tiếng cùng ớt bột Hàn Quốc 7 cấp độ" });
            }
            else
            {
                ingredients.AddRange(new[] { "Nguyên liệu tươi sạch nhập mới mỗi ngày", "Dầu thực vật chiên nóng sốt tại quầy", "Gia vị thảo mộc tự nhiên", "Rau thơm và dưa chua ăn kèm chuẩn ATVSTP" });
            }
        }
        else
        {
            if (nameLower.Contains("matcha"))
            {
                ingredients.AddRange(new[] { "Bột trà xanh Matcha Uji nhập khẩu Nhật Bản nguyên chất", "Sữa tươi thanh trùng Đà Lạt Milk / VietGAP", "Đường mía organic cô đặc thanh ngọt", "Đá viên lọc RO tiệt trùng" });
            }
            else if (nameLower.Contains("đào") || nameLower.Contains("cam sả") || nameLower.Contains("trái cây") || nameLower.Contains("hoa quả") || nameLower.Contains("dâu"))
            {
                ingredients.AddRange(new[] { "Cốt hồng trà hảo hạng ủ mới mỗi 4 giờ", "Trái cây tươi tự nhiên (Đào / Cam vàng / Dâu tây)", "Sả tươi đập dập thơm dịu nhẹ", "Nước cốt quả tươi ép nguyên chất", "Mật ong hoa rừng tự nhiên" });
            }
            else if (nameLower.Contains("kem cheese") || nameLower.Contains("macchiato"))
            {
                ingredients.AddRange(new[] { "Cốt trà Ô Long hoặc Trà Xanh ủ lạnh giữ trọn hương", "Lớp kem phô mai Cream Cheese Anchor New Zealand béo ngậy", "Sữa tươi thanh trùng", "Chút muối hồng Himalaya tạo hậu vị đậm đà độc đáo" });
            }
            else if (nameLower.Contains("cà phê") || nameLower.Contains("cafe"))
            {
                ingredients.AddRange(new[] { "Hạt Robusta & Arabica Cầu Đất rang mộc đậm đà", "Sữa đặc béo thơm truyền thống", "Kem muối biển bồng bềnh đặc chế MilkyVerse", "Đá tinh khiết" });
            }
            else
            {
                ingredients.AddRange(new[] { "Lá trà Ô Long Mộc Châu tuyển chọn sao sấy thủ công", "Sữa tươi thanh trùng tiệt trùng VietGAP", "Bột kem béo thực vật không đường lactose tốt cho tiêu hóa", "Trân châu hoàng kim nấu đường đen mía nguyên chất", "Nước lọc tinh khiết RO qua 5 tầng lọc" });
            }
        }

        string tastingGuide = p.ProductType == "FOOD"
            ? "Thưởng thức ngon nhất khi món ăn vừa được chế biến nóng giòn. Chấm cùng nước sốt đặc chế đi kèm để cảm nhận trọn vẹn sự hòa quyện gia vị bùng nổ vị giác!"
            : "• 0% Đường: Vị trà mộc mạc, đậm đà, hơi chát nhẹ hậu ngọt thanh (dành cho người ăn kiêng / healthy).\n• 30% Đường: Vị ngọt nhẹ nhàng thanh mát, giữ trọn 100% nốt hương hoa trà.\n• 50% Đường: (Khuyên Dùng - Chuẩn MilkyVerse) Cân bằng hoàn mỹ giữa vị đậm của trà và béo ngậy của sữa tươi.\n• 70% Đường: Ngọt dịu êm, thơm béo hài hòa.\n• 100% Đường: Ngọt đậm đà truyền thống, thích hợp uống khi tan cùng nhiều đá lạnh.";

        var detail = new ProductDetailDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : "",
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            BasePrice = p.BasePrice,
            ImageUrl = p.ImageUrl,
            ImagePlaceholder = p.ImagePlaceholder,
            IsBestSeller = p.IsBestSeller,
            IsNew = p.IsNew,
            IsActive = p.IsActive,
            StockStatus = p.StockStatus,
            ProductType = p.ProductType,
            SpicyLevel = p.SpicyLevel,
            AllowSpicyCustomization = p.AllowSpicyCustomization,
            AllowSauceCustomization = p.AllowSauceCustomization,
            DefaultSauce = p.DefaultSauce,
            PortionDescription = p.PortionDescription,
            AvailableToppings = toppings,
            ComboItems = comboItems,
            Sizes = p.ProductType == "DRINK" ? new List<string> { "M", "L" } : new List<string> { "Standard" },
            SugarLevels = new List<string> { "0%", "30%", "50%", "70%", "100%" },
            IceLevels = new List<string> { "0% (Không đá)", "30%", "50%", "70%", "100% (Đá chuẩn)" },
            SpicyOptions = new List<string> { "Cấp 0 - Không cay", "Cấp 1 - Cay nhẹ", "Cấp 2 - Cay vừa", "Cấp 3 - Cay nồng", "Cấp 5 - Rất cay", "Cấp 7 - Siêu cay" },
            SauceOptions = new List<string> { "Tương ớt cay nồng", "Sốt me chua ngọt đặc chế", "Sốt bơ trứng béo ngậy", "Muối tiêu chanh ớt xanh", "Sốt phô mai cay", "Tương đen xào sa tế" },
            PortionOptions = new List<string> { "Phần tiêu chuẩn", "Phần lớn (+15.000đ)", "Phần gia đình / khổng lồ (+35.000đ)" },
            RelatedProducts = related,
            Ingredients = ingredients,
            TastingGuide = tastingGuide,
            AverageRating = avgRating,
            TotalReviews = reviews.Count,
            Reviews = reviews
        };

        return ApiResponse<ProductDetailDto>.Ok(detail);
    }

    public async Task<ApiResponse<List<ToppingDto>>> GetToppingsAsync()
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

        return ApiResponse<List<ToppingDto>>.Ok(toppings);
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug) 
            ? request.Name.ToLower().Replace(" ", "-") 
            : request.Slug.ToLower().Replace(" ", "-");

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            BasePrice = request.BasePrice,
            ImageUrl = request.ImageUrl,
            IsBestSeller = request.IsBestSeller,
            IsNew = request.IsNew,
            IsActive = true,
            StockStatus = request.StockStatus,
            ProductType = request.ProductType,
            SpicyLevel = request.SpicyLevel,
            AllowSpicyCustomization = request.AllowSpicyCustomization,
            AllowSauceCustomization = request.AllowSauceCustomization,
            DefaultSauce = request.DefaultSauce,
            PortionDescription = request.PortionDescription
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return ApiResponse<ProductDto>.Ok(new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            ImageUrl = product.ImageUrl,
            IsBestSeller = product.IsBestSeller,
            IsNew = product.IsNew,
            IsActive = product.IsActive,
            StockStatus = product.StockStatus,
            ProductType = product.ProductType,
            SpicyLevel = product.SpicyLevel,
            AllowSpicyCustomization = product.AllowSpicyCustomization,
            AllowSauceCustomization = product.AllowSauceCustomization,
            DefaultSauce = product.DefaultSauce,
            PortionDescription = product.PortionDescription
        }, "Tạo món thành công!");
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return ApiResponse<ProductDto>.Fail("Không tìm thấy món.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Description = request.Description;
        product.BasePrice = request.BasePrice;
        product.ImageUrl = request.ImageUrl;
        product.IsBestSeller = request.IsBestSeller;
        product.IsNew = request.IsNew;
        product.IsActive = request.IsActive;
        product.StockStatus = request.StockStatus;
        product.ProductType = request.ProductType;
        product.SpicyLevel = request.SpicyLevel;
        product.AllowSpicyCustomization = request.AllowSpicyCustomization;
        product.AllowSauceCustomization = request.AllowSauceCustomization;
        product.DefaultSauce = request.DefaultSauce;
        product.PortionDescription = request.PortionDescription;

        await _context.SaveChangesAsync();

        return ApiResponse<ProductDto>.Ok(new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BasePrice = product.BasePrice,
            ImageUrl = product.ImageUrl,
            IsBestSeller = product.IsBestSeller,
            IsNew = product.IsNew,
            IsActive = product.IsActive,
            StockStatus = product.StockStatus,
            ProductType = product.ProductType,
            SpicyLevel = product.SpicyLevel,
            AllowSpicyCustomization = product.AllowSpicyCustomization,
            AllowSauceCustomization = product.AllowSauceCustomization,
            DefaultSauce = product.DefaultSauce,
            PortionDescription = product.PortionDescription
        }, "Cập nhật món thành công!");
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy món.");
        }

        product.IsActive = false;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Đã ngừng kinh doanh món này.");
    }

    public async Task<ApiResponse<List<ProductReviewDto>>> GetReviewsAsync(int productId)
    {
        try
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.UserName,
                    UserAvatar = r.UserAvatar,
                    Rating = r.Rating,
                    Verdict = r.Verdict,
                    Comment = r.Comment,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<ProductReviewDto>>.Ok(reviews);
        }
        catch
        {
            return ApiResponse<List<ProductReviewDto>>.Ok(new List<ProductReviewDto>());
        }
    }

    public async Task<ApiResponse<List<ProductReviewDto>>> GetAllRecentReviewsAsync(int limit = 10)
    {
        try
        {
            var reviews = await _context.ProductReviews
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .Select(r => new ProductReviewDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.UserName,
                    UserAvatar = r.UserAvatar,
                    Rating = r.Rating,
                    Verdict = r.Verdict,
                    Comment = r.Comment,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<ProductReviewDto>>.Ok(reviews);
        }
        catch
        {
            return ApiResponse<List<ProductReviewDto>>.Ok(new List<ProductReviewDto>());
        }
    }

    public async Task<ApiResponse<ProductReviewDto>> AddReviewAsync(int productId, int userId, string userName, string? userAvatar, CreateProductReviewRequest request)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return ApiResponse<ProductReviewDto>.Fail("Không tìm thấy sản phẩm.");
        }

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "Khách Hàng MilkyVerse" : userName,
            UserAvatar = userAvatar,
            Rating = Math.Clamp(request.Rating, 1, 5),
            Verdict = string.IsNullOrWhiteSpace(request.Verdict) ? "Rất ngon" : request.Verdict,
            Comment = request.Comment?.Trim() ?? string.Empty,
            IsVerifiedPurchase = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        var dto = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = review.UserName,
            UserAvatar = review.UserAvatar,
            Rating = review.Rating,
            Verdict = review.Verdict,
            Comment = review.Comment,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            CreatedAt = review.CreatedAt
        };

        return ApiResponse<ProductReviewDto>.Ok(dto, "Cảm ơn bạn đã gửi đánh giá cho món ăn!");
    }
}
