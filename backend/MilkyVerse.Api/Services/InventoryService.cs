using Microsoft.EntityFrameworkCore;
using MilkyVerse.Api.Data;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services;

public interface IInventoryService
{
    Task<ApiResponse<List<IngredientDto>>> GetIngredientsAsync();
    Task<ApiResponse<IngredientDto>> CreateIngredientAsync(IngredientDto dto);
    Task<ApiResponse<IngredientDto>> UpdateIngredientAsync(int id, IngredientDto dto);
    Task<ApiResponse<bool>> AdjustStockAsync(StockAdjustRequest request);
    Task<ApiResponse<List<InventoryTransaction>>> GetTransactionsAsync(int? ingredientId = null);

    // FEFO Batches
    Task<ApiResponse<List<StockBatchDto>>> GetStockBatchesAsync(int? ingredientId = null);

    // Purchase Receipts
    Task<ApiResponse<List<PurchaseReceiptResponseDto>>> GetPurchaseReceiptsAsync();
    Task<ApiResponse<PurchaseReceiptResponseDto>> CreatePurchaseReceiptAsync(int? userId, CreatePurchaseReceiptRequest request);

    // Stock Audits
    Task<ApiResponse<List<StockAuditResponseDto>>> GetStockAuditsAsync();
    Task<ApiResponse<StockAuditResponseDto>> CreateStockAuditAsync(int? userId, CreateStockAuditRequest request);
}

public class InventoryService : IInventoryService
{
    private readonly MilkyVerseDbContext _context;

    public InventoryService(MilkyVerseDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<IngredientDto>>> GetIngredientsAsync()
    {
        var ingredients = await _context.Ingredients
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
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
            .ToListAsync();

        return ApiResponse<List<IngredientDto>>.Ok(ingredients);
    }

    public async Task<ApiResponse<IngredientDto>> CreateIngredientAsync(IngredientDto dto)
    {
        var ing = new Ingredient
        {
            Name = dto.Name,
            Unit = dto.Unit,
            CurrentStock = dto.CurrentStock,
            MinAlertThreshold = dto.MinAlertThreshold,
            UnitCost = dto.UnitCost,
            CategoryType = string.IsNullOrWhiteSpace(dto.CategoryType) ? "BEVERAGE" : dto.CategoryType,
            IsActive = true
        };

        _context.Ingredients.Add(ing);
        await _context.SaveChangesAsync();

        dto.Id = ing.Id;
        return ApiResponse<IngredientDto>.Ok(dto, "Thêm nguyên liệu mới thành công!");
    }

    public async Task<ApiResponse<IngredientDto>> UpdateIngredientAsync(int id, IngredientDto dto)
    {
        var ing = await _context.Ingredients.FindAsync(id);
        if (ing == null)
        {
            return ApiResponse<IngredientDto>.Fail("Không tìm thấy nguyên liệu.");
        }

        ing.Name = dto.Name;
        ing.Unit = dto.Unit;
        ing.MinAlertThreshold = dto.MinAlertThreshold;
        ing.UnitCost = dto.UnitCost;
        if (!string.IsNullOrWhiteSpace(dto.CategoryType)) ing.CategoryType = dto.CategoryType;

        await _context.SaveChangesAsync();
        return ApiResponse<IngredientDto>.Ok(dto, "Cập nhật nguyên liệu thành công!");
    }

    public async Task<ApiResponse<bool>> AdjustStockAsync(StockAdjustRequest request)
    {
        var ing = await _context.Ingredients.FindAsync(request.IngredientId);
        if (ing == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy nguyên liệu.");
        }

        decimal oldStock = ing.CurrentStock;
        decimal delta = request.Quantity;

        if (request.TransactionType == "EXPORT")
        {
            delta = -Math.Abs(delta);
        }
        else if (request.TransactionType == "IMPORT")
        {
            delta = Math.Abs(delta);
        }

        ing.CurrentStock = Math.Max(0, ing.CurrentStock + delta);

        var tx = new InventoryTransaction
        {
            IngredientId = ing.Id,
            TransactionType = request.TransactionType,
            Quantity = delta,
            BalanceAfter = ing.CurrentStock,
            ReferenceType = "Manual",
            ReferenceId = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? $"Điều chỉnh kho: {oldStock} -> {ing.CurrentStock}" : request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _context.InventoryTransactions.Add(tx);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Cập nhật tồn kho thành công!");
    }

    public async Task<ApiResponse<List<InventoryTransaction>>> GetTransactionsAsync(int? ingredientId = null)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Ingredient)
            .AsQueryable();

        if (ingredientId.HasValue && ingredientId.Value > 0)
        {
            query = query.Where(t => t.IngredientId == ingredientId.Value);
        }

        var list = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .ToListAsync();

        return ApiResponse<List<InventoryTransaction>>.Ok(list);
    }

    public async Task<ApiResponse<List<StockBatchDto>>> GetStockBatchesAsync(int? ingredientId = null)
    {
        var query = _context.StockBatches
            .Include(b => b.Ingredient)
            .AsQueryable();

        if (ingredientId.HasValue && ingredientId.Value > 0)
        {
            query = query.Where(b => b.IngredientId == ingredientId.Value);
        }

        // FEFO: First Expired First Out -> order by ExpiryDate ascending
        var batches = await query
            .OrderBy(b => b.ExpiryDate)
            .Select(b => new StockBatchDto
            {
                Id = b.Id,
                IngredientId = b.IngredientId,
                IngredientName = b.Ingredient != null ? b.Ingredient.Name : string.Empty,
                Unit = b.Ingredient != null ? b.Ingredient.Unit : string.Empty,
                BatchNumber = b.BatchNumber,
                ManufactureDate = b.ManufactureDate,
                ExpiryDate = b.ExpiryDate,
                InitialQuantity = b.InitialQuantity,
                CurrentQuantity = b.CurrentQuantity,
                UnitCost = b.UnitCost
            })
            .ToListAsync();

        return ApiResponse<List<StockBatchDto>>.Ok(batches);
    }

    public async Task<ApiResponse<List<PurchaseReceiptResponseDto>>> GetPurchaseReceiptsAsync()
    {
        var receipts = await _context.PurchaseReceipts
            .Include(pr => pr.Items)
            .OrderByDescending(pr => pr.CreatedAt)
            .Select(pr => new PurchaseReceiptResponseDto
            {
                Id = pr.Id,
                ReceiptCode = pr.ReceiptCode,
                SupplierName = pr.SupplierName,
                TotalCost = pr.TotalCost,
                Status = pr.Status,
                Notes = pr.Notes,
                CreatedAt = pr.CreatedAt,
                ItemCount = pr.Items.Count
            })
            .ToListAsync();

        return ApiResponse<List<PurchaseReceiptResponseDto>>.Ok(receipts);
    }

    public async Task<ApiResponse<PurchaseReceiptResponseDto>> CreatePurchaseReceiptAsync(int? userId, CreatePurchaseReceiptRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return ApiResponse<PurchaseReceiptResponseDto>.Fail("Phiếu nhập kho phải có ít nhất 1 mặt hàng.");
        }

        var receiptCode = "PNK" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper();
        decimal totalCost = 0;

        var receipt = new PurchaseReceipt
        {
            ReceiptCode = receiptCode,
            SupplierName = string.IsNullOrWhiteSpace(request.SupplierName) ? "Nhà cung cấp tổng hợp" : request.SupplierName,
            CreatedByUserId = userId,
            Notes = request.Notes,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow
        };

        _context.PurchaseReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var ing = await _context.Ingredients.FindAsync(item.IngredientId);
            if (ing != null)
            {
                decimal itemCost = item.Quantity * item.UnitPrice;
                totalCost += itemCost;

                // Create Receipt Item
                var receiptItem = new PurchaseReceiptItem
                {
                    PurchaseReceiptId = receipt.Id,
                    IngredientId = ing.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    BatchNumber = string.IsNullOrWhiteSpace(item.BatchNumber) ? $"LOT-{DateTime.UtcNow:yyyyMMdd}-{ing.Id}" : item.BatchNumber,
                    ExpiryDate = item.ExpiryDate
                };
                _context.PurchaseReceiptItems.Add(receiptItem);

                // Create FEFO batch
                _context.StockBatches.Add(new StockBatch
                {
                    IngredientId = ing.Id,
                    BatchNumber = receiptItem.BatchNumber,
                    ManufactureDate = DateTime.UtcNow,
                    ExpiryDate = item.ExpiryDate,
                    InitialQuantity = item.Quantity,
                    CurrentQuantity = item.Quantity,
                    UnitCost = item.UnitPrice,
                    CreatedAt = DateTime.UtcNow
                });

                // Update ingredient stock & cost
                ing.CurrentStock += item.Quantity;
                if (item.UnitPrice > 0) ing.UnitCost = item.UnitPrice;

                // Record transaction
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    IngredientId = ing.Id,
                    TransactionType = "IMPORT",
                    Quantity = item.Quantity,
                    BalanceAfter = ing.CurrentStock,
                    ReferenceType = "PurchaseReceipt",
                    ReferenceId = receiptCode,
                    Note = $"Nhập kho theo phiếu {receiptCode}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        receipt.TotalCost = totalCost;
        await _context.SaveChangesAsync();

        return ApiResponse<PurchaseReceiptResponseDto>.Ok(new PurchaseReceiptResponseDto
        {
            Id = receipt.Id,
            ReceiptCode = receipt.ReceiptCode,
            SupplierName = receipt.SupplierName,
            TotalCost = receipt.TotalCost,
            Status = receipt.Status,
            Notes = receipt.Notes,
            CreatedAt = receipt.CreatedAt,
            ItemCount = request.Items.Count
        }, "Tạo phiếu nhập kho thành công!");
    }

    public async Task<ApiResponse<List<StockAuditResponseDto>>> GetStockAuditsAsync()
    {
        var audits = await _context.StockAudits
            .Include(a => a.Items)
                .ThenInclude(i => i.Ingredient)
            .OrderByDescending(a => a.AuditDate)
            .Select(a => new StockAuditResponseDto
            {
                Id = a.Id,
                AuditCode = a.AuditCode,
                AuditDate = a.AuditDate,
                Status = a.Status,
                Notes = a.Notes,
                Items = a.Items.Select(i => new StockAuditItemResponseDto
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.Ingredient != null ? i.Ingredient.Name : string.Empty,
                    Unit = i.Ingredient != null ? i.Ingredient.Unit : string.Empty,
                    SystemQuantity = i.SystemQuantity,
                    ActualQuantity = i.ActualQuantity,
                    Variance = i.Variance,
                    Reason = i.Reason
                }).ToList()
            })
            .ToListAsync();

        return ApiResponse<List<StockAuditResponseDto>>.Ok(audits);
    }

    public async Task<ApiResponse<StockAuditResponseDto>> CreateStockAuditAsync(int? userId, CreateStockAuditRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return ApiResponse<StockAuditResponseDto>.Fail("Phiếu kiểm kê phải có ít nhất 1 mặt hàng.");
        }

        var auditCode = "KK" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..4].ToUpper();

        var audit = new StockAudit
        {
            AuditCode = auditCode,
            AuditedByUserId = userId,
            AuditDate = DateTime.UtcNow,
            Status = "Completed",
            Notes = request.Notes
        };

        _context.StockAudits.Add(audit);
        await _context.SaveChangesAsync();

        var auditResponseItems = new List<StockAuditItemResponseDto>();

        foreach (var item in request.Items)
        {
            var ing = await _context.Ingredients.FindAsync(item.IngredientId);
            if (ing != null)
            {
                decimal systemQty = ing.CurrentStock;
                decimal variance = item.ActualQuantity - systemQty;

                var auditItem = new StockAuditItem
                {
                    StockAuditId = audit.Id,
                    IngredientId = ing.Id,
                    SystemQuantity = systemQty,
                    ActualQuantity = item.ActualQuantity,
                    Variance = variance,
                    Reason = item.Reason
                };
                _context.StockAuditItems.Add(auditItem);

                // Update stock to actual
                ing.CurrentStock = item.ActualQuantity;

                if (variance != 0)
                {
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        IngredientId = ing.Id,
                        TransactionType = "ADJUST",
                        Quantity = variance,
                        BalanceAfter = ing.CurrentStock,
                        ReferenceType = "StockAudit",
                        ReferenceId = auditCode,
                        Note = $"Cân chỉnh kho sau kiểm kê {auditCode}: {variance:+0.##;-0.##;0} ({item.Reason})",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                auditResponseItems.Add(new StockAuditItemResponseDto
                {
                    IngredientId = ing.Id,
                    IngredientName = ing.Name,
                    Unit = ing.Unit,
                    SystemQuantity = systemQty,
                    ActualQuantity = item.ActualQuantity,
                    Variance = variance,
                    Reason = item.Reason
                });
            }
        }

        await _context.SaveChangesAsync();

        return ApiResponse<StockAuditResponseDto>.Ok(new StockAuditResponseDto
        {
            Id = audit.Id,
            AuditCode = audit.AuditCode,
            AuditDate = audit.AuditDate,
            Status = audit.Status,
            Notes = audit.Notes,
            Items = auditResponseItems
        }, "Tạo phiếu kiểm kê và cân bằng kho thành công!");
    }
}
