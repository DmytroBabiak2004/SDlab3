using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class DateTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
{
    private const int BatchSize = 1000;

    public async Task TransferDatesAsync()
    {
        var dateCount = await sourceContext.Sales
            .Select(s => s.SaleDate.Date)
            .Distinct()
            .CountAsync();
        var processedCount = LoggerService.GetProcessedCount(nameof(DimDate));

        while (processedCount < dateCount)
        {
            await AddDates(await GetDates(sourceContext.Sales, processedCount));

            processedCount += BatchSize;
            LoggerService.RecordTransferCount(processedCount, nameof(DimDate));
        }
    }

    public async Task TransferNewDatesAsync()
    {
        var latestDateId = await targetContext.DimDates
            .OrderByDescending(d => d.DateId)
            .Select(d => d.DateId)
            .FirstOrDefaultAsync();

        var latestDate = await targetContext.DimDates
            .Where(d => d.DateId == latestDateId)
            .Select(d => new DateTime(d.Year, d.Month, d.Day))
            .FirstOrDefaultAsync();

        // Завантажуємо всі існуючі дати з DimDates у пам’ять
        var existingDates = await targetContext.DimDates
            .Select(d => new { d.Day, d.Month, d.Year })
            .ToListAsync();

        while (true)
        {
            // Фільтруємо Sales, використовуючи existingDates у пам’яті
            var newDates = sourceContext.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate.Date >= latestDate)
                .AsEnumerable() // Переходимо до клієнтської оцінки для Contains
                .Where(s => !existingDates.Any(d => d.Day == s.SaleDate.Day &&
                                                   d.Month == s.SaleDate.Month &&
                                                   d.Year == s.SaleDate.Year))
                .Select(s => s.SaleDate)
                .Distinct()
                .OrderBy(d => d)
                .Take(BatchSize)
                .Select(d => Mapper.MapToDimDate(d))
                .ToList();

            if (newDates.Count() == 0)
                break;

            await AddDates(newDates);
            latestDate = newDates.Max(d => new DateTime(d.Year, d.Month, d.Day));
        }
    }

    private async Task AddDates(List<DimDate> dates)
    {
        await targetContext.DimDates.AddRangeAsync(dates);
        await targetContext.SaveChangesAsync();
    }

    private static async Task<List<DimDate>> GetDates(IQueryable<Sale> sales, int skip = 0)
    {
        return await sales
            .AsNoTracking()
            .Select(s => s.SaleDate)
            .Distinct()
            .OrderBy(d => d)
            .Skip(skip)
            .Take(BatchSize)
            .Select(d => Mapper.MapToDimDate(d))
            .ToListAsync();
    }
}