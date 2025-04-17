using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class ProductTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
{
    private const int BatchSize = 1000;

    public async Task TransferProductsAsync()
    {
        var productCount = await sourceContext.Products.CountAsync();
        var processedCount = LoggerService.GetProcessedCount(nameof(DimProduct));

        var existingProductIds = targetContext.DimProducts
            .AsNoTracking()
            .Select(p => p.ProductId)
            .ToHashSet();

        while (processedCount < productCount)
        {
            var products = await GetProductsAsync(sourceContext.Products, processedCount);

            var newProducts = products
                .Where(p => !existingProductIds.Contains(p.ProductId))
                .ToList();

            if (newProducts.Count > 0)
            {
                await targetContext.DimProducts.AddRangeAsync(newProducts);
                await targetContext.SaveChangesAsync();
            }

            processedCount += BatchSize;
            LoggerService.RecordTransferCount(processedCount, nameof(DimProduct));
        }
    }

    public async Task TransferNewProductsAsync()
    {
        var existingProductIds = targetContext.DimProducts
            .AsNoTracking()
            .Select(p => p.ProductId)
            .ToHashSet();

        var newProducts = await sourceContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory)
            .Include(p => p.Brand)
            .Where(p => !existingProductIds.Contains(p.ProductId))
            .ToListAsync();

        int total = newProducts.Count;
        int processed = 0;

        while (processed < total)
        {
            var batch = newProducts
                .Skip(processed)
                .Take(BatchSize)
                .ToList();

            var dimProducts = GetProductsFromList(batch);

            await targetContext.DimProducts.AddRangeAsync(dimProducts);
            await targetContext.SaveChangesAsync();

            processed += BatchSize;
            LoggerService.RecordTransferCount(processed, nameof(DimProduct));
        }
    }

    private static async Task<List<DimProduct>> GetProductsAsync(IQueryable<Product> queryableProducts, int skip = 0)
    {
        var products = await queryableProducts
            .AsNoTracking()
            .Include(p => p.Category)
                .ThenInclude(c => c.ParentCategory)
            .Include(p => p.Brand)
            .OrderBy(p => p.ProductId)
            .Skip(skip)
            .Take(BatchSize)
            .ToListAsync();

        return GetProductsFromList(products);
    }

    private static List<DimProduct> GetProductsFromList(List<Product> products)
    {
        return products.Select(p => new DimProduct
        {
            ProductId = p.ProductId,
            ProductName = p.Name,
            Category = p.Category?.Name,
            ParentCategory = p.Category?.ParentCategory?.Name,
            Brand = p.Brand?.BrandName,
            Origin = p.Brand?.Origin
        }).ToList();
    }
}
