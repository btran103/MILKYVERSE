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
            Sizes = p.ProductType == "DRINK" ? new List<string> { "M", "L" } : new List<string> { "Standard" },
            SugarLevels = new List<string> { "0%", "30%", "50%", "70%", "100%" },
            IceLevels = new List<string> { "0% (Không đá)", "30%", "50%", "70%", "100% (Đá chuẩn)" },
            SpicyOptions = new List<string> { "Cấp 0 - Không cay", "Cấp 1 - Cay nhẹ", "Cấp 2 - Cay vừa", "Cấp 3 - Cay nồng", "Cấp 5 - Rất cay", "Cấp 7 - Siêu cay" },
            SauceOptions = new List<string> { "Tương ớt cay nồng", "Sốt me chua ngọt đặc chế", "Sốt bơ trứng béo ngậy", "Muối tiêu chanh ớt xanh", "Sốt phô mai cay", "Tương đen xào sa tế" },
            PortionOptions = new List<string> { "Phần tiêu chuẩn", "Phần lớn (+15.000đ)", "Phần gia đình / khổng lồ (+35.000đ)" },
            RelatedProducts = related
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
}
