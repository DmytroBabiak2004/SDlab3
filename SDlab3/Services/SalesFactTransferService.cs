using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class SalesFactTransferService
{
    private readonly StoretransactionsdbContext _sourceContext;
    private readonly OlapstoreTransactionDbContext _targetContext;
    private const int BatchSize = 1000;

    public SalesFactTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
    {
        _sourceContext = sourceContext;
        _targetContext = targetContext;
    }
    public async Task TransferSalesFactsAsync()
    {
        var minDate = await _sourceContext.Sales.MinAsync(s => s.SaleDate);
        var maxDate = await _sourceContext.Sales.MaxAsync(s => s.SaleDate);

        var currentDate = minDate;

        while (currentDate <= maxDate)
        {
            var sales = await GetSalesFacts(
                _sourceContext.Sales.Where(s => s.SaleDate == currentDate),
                0); // íå skipàºìî, áî âæå ô³ëüòðóºìî ïî äàò³

            if (sales.Count() == 0)
            {
                currentDate = currentDate.AddDays(1);
                continue;
            }

            await _targetContext.SalesFacts.AddRangeAsync(sales);
            await _targetContext.SaveChangesAsync();

            LoggerService.RecordTransferCount(sales.Count(), nameof(SalesFact));
            currentDate = currentDate.AddDays(1);
        }
    }

    public async Task TransferNewSalesFactsAsync()
    {
        var existingSaleIds = await _targetContext.SalesFacts
            .Select(s => s.SaleId)
            .ToListAsync();

        var minDate = await _sourceContext.Sales.MinAsync(s => s.SaleDate);
        var maxDate = await _sourceContext.Sales.MaxAsync(s => s.SaleDate);

        var currentDate = minDate;

        while (currentDate <= maxDate)
        {
            var salesQuery = _sourceContext.Sales
                .Where(s => s.SaleDate == currentDate && !existingSaleIds.Contains(s.SaleId));

            int processedCount = 0;

            while (true)
            {
                var sales = await GetSalesFacts(salesQuery, processedCount);
                if (sales.Count() == 0)
                    break;

                await _targetContext.SalesFacts.AddRangeAsync(sales);
                await _targetContext.SaveChangesAsync();

                foreach (var sale in sales)
                {
                    existingSaleIds.Add(sale.SaleId);
                }

                processedCount += sales.Count();
            }

            currentDate = currentDate.AddDays(1);
        }
    }


    private async Task<List<SalesFact>> GetSalesFacts(IQueryable<Sale> sales, int skip)
    {
        var salesList = await sales
            .AsNoTracking()
            .Include(s => s.DeptCodeNavigation)
            .Include(s => s.Product)
            .OrderBy(s => s.SaleId)
            .Skip(skip)
            .Take(BatchSize)
            .ToListAsync();

        var dateLookup = await _targetContext.DimDates
            .ToDictionaryAsync(d => d.DateId, d => new { d.Day, d.Month, d.Year });

        var transactionLookup = await _targetContext.TransactionTypes
            .ToDictionaryAsync(t => t.TransactionTypeId, t => new { t.IsCashless, t.IsReturned });

        var result = new List<SalesFact>();

        foreach (var s in salesList)
        {
            var dateMatch = dateLookup.FirstOrDefault(d => d.Value.Day == s.SaleDate.Day &&
                                                          d.Value.Month == s.SaleDate.Month &&
                                                          d.Value.Year == s.SaleDate.Year);

            long dateId;
            if (dateMatch.Key == 0)
            {
                var newDate = Mapper.MapToDimDate(s.SaleDate);
                _targetContext.DimDates.Add(newDate);
                await _targetContext.SaveChangesAsync();
                dateId = newDate.DateId;
                dateLookup[dateId] = new { newDate.Day, newDate.Month, newDate.Year };
            }
            else
            {
                dateId = dateMatch.Key;
            }

            var transactionMatch = transactionLookup.FirstOrDefault(t => t.Value.IsCashless == s.IsCashless &&
                                                                       t.Value.IsReturned == !string.IsNullOrEmpty(s.ReturnReason));
            if (transactionMatch.Key == 0)
                throw new KeyNotFoundException($"No TransactionTypeId found for IsCashless={s.IsCashless}, IsReturned={!string.IsNullOrEmpty(s.ReturnReason)}");

            result.Add(Mapper.MapToSalesFact(
                s.SaleId,
                s,
                dateId,
                s.DeptCode ?? "Unknown",
                s.ProductId,
                transactionMatch.Key
            ));
        }

        return result;
    }

}