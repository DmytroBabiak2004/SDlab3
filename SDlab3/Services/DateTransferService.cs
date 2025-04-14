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
            var datesToAdd = await GetDates(sourceContext.Sales, processedCount);
            await AddDates(datesToAdd);

            processedCount += BatchSize;
            LoggerService.RecordTransferCount(processedCount, nameof(DimDate));
        }
    }

    public async Task TransferNewDatesAsync()
    {
        var latestDate = await targetContext.DimDates
            .OrderByDescending(d => new DateTime(d.Year, d.Month, d.Day))
            .Select(d => new DateTime(d.Year, d.Month, d.Day))
            .FirstOrDefaultAsync();

        while (true)
        {
            // Витягуємо нові дати з джерела
            var newDates = sourceContext.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate.Date > latestDate)
                .Select(s => s.SaleDate.Date)
                .Distinct()
                .OrderBy(d => d)
                .Take(BatchSize)
                .ToList();

            if (newDates.Count == 0)
                break;

            var mappedDates = newDates.Select(Mapper.MapToDimDate).ToList();
            await AddDates(mappedDates);

            latestDate = newDates.Max();
        }
    }

    private async Task AddDates(List<DimDate> dates)
    {
        if (dates.Count == 0)
            return;

        // Отримуємо існуючі дати з БД
        var existingDates = await targetContext.DimDates
            .Select(d => new { d.Day, d.Month, d.Year })
            .ToListAsync();

        // Фільтруємо ті, що ще не існують
        var newDates = dates
            .Where(d => !existingDates.Any(e =>
                e.Day == d.Day &&
                e.Month == d.Month &&
                e.Year == d.Year))
            .ToList();

        if (newDates.Count > 0)
        {
            await targetContext.DimDates.AddRangeAsync(newDates);
            await targetContext.SaveChangesAsync();
        }
    }

    private static async Task<List<DimDate>> GetDates(IQueryable<Sale> sales, int skip = 0)
    {
        var dates = await sales
            .AsNoTracking()
            .Select(s => s.SaleDate.Date)
            .Distinct()
            .OrderBy(d => d)
            .Skip(skip)
            .Take(BatchSize)
            .ToListAsync();

        return dates.Select(Mapper.MapToDimDate).ToList();
    }
}
