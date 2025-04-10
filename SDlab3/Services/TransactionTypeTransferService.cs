using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class TransactionTypeTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
{
    private const int BatchSize = 1000;

    public async Task TransferTransactionTypesAsync()
    {
        var transactionCount = await sourceContext.Sales
            .Select(s => new { s.IsCashless, s.ReturnReason })
            .Distinct()
            .CountAsync();

        var processedCount = LoggerService.GetProcessedCount(nameof(TransactionType));

        while (processedCount < transactionCount)
        {
            var transactionTypes = await GetTransactionTypes(sourceContext.Sales, processedCount);

            await targetContext.TransactionTypes.AddRangeAsync(transactionTypes);
            await targetContext.SaveChangesAsync();

            processedCount += BatchSize;
            LoggerService.RecordTransferCount(processedCount, nameof(TransactionType));
        }
    }

    public async Task TransferNewTransactionTypesAsync()
    {
        // 1. Завантажуємо всі унікальні комбінації з OLTP
        var allDistinctTypes = await sourceContext.Sales
            .AsNoTracking()
            .Select(s => new { s.IsCashless, IsReturned = !string.IsNullOrEmpty(s.ReturnReason) })
            .Distinct()
            .ToListAsync();

        // 2. Завантажуємо вже перенесені комбінації
        var existingTypes = await targetContext.TransactionTypes
            .AsNoTracking()
            .Select(t => new { t.IsCashless, t.IsReturned })
            .ToListAsync();

        // 3. Фільтруємо нові типи вручну
        var newTypes = allDistinctTypes
            .Where(nt => !existingTypes
                .Any(et => et.IsCashless == nt.IsCashless && et.IsReturned == nt.IsReturned))
            .ToList();

        int total = newTypes.Count;
        int processed = 0;

        while (processed < total)
        {
            var batch = newTypes
                .Skip(processed)
                .Take(BatchSize)
                .Select(s => Mapper.MapToTransactionType(new Sale
                {
                    IsCashless = s.IsCashless,
                    ReturnReason = s.IsReturned ? "some value" : null
                }))
                .ToList();

            await targetContext.TransactionTypes.AddRangeAsync(batch);
            await targetContext.SaveChangesAsync();

            processed += BatchSize;
            LoggerService.RecordTransferCount(processed, nameof(TransactionType));
        }
    }

    private static async Task<List<TransactionType>> GetTransactionTypes(IQueryable<Sale> sales, int skip = 0)
    {
        return await sales
            .AsNoTracking()
            .Select(s => new { s.IsCashless, s.ReturnReason })
            .Distinct()
            .Skip(skip)
            .Take(BatchSize)
            .Select(s => Mapper.MapToTransactionType(new Sale
            {
                IsCashless = s.IsCashless,
                ReturnReason = s.ReturnReason
            }))
            .ToListAsync();
    }
}
