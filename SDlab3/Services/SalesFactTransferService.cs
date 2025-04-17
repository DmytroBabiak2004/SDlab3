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
        var existingSaleIds = await _targetContext.SalesFacts
            .Select(s => s.SaleId)
            .ToListAsync();

        var minDate = await _sourceContext.Sales.MinAsync(s => s.SaleDate);
        var maxDate = await _sourceContext.Sales.MaxAsync(s => s.SaleDate);

        var dateLookup = await LoadDateLookupAsync();
        var transactionLookup = await LoadTransactionLookupAsync();

        for (var currentDate = minDate.Date; currentDate <= maxDate.Date; currentDate = currentDate.AddDays(1))
        {
            var salesQuery = _sourceContext.Sales
                .Where(s => s.SaleDate.Date == currentDate && !existingSaleIds.Contains(s.SaleId));

            int processedCount = 0;
            while (true)
            {
                var sales = await GetSalesFactsAsync(salesQuery, processedCount, dateLookup, transactionLookup);
                if (sales.Count == 0)
                    break;

                await _targetContext.SalesFacts.AddRangeAsync(sales);
                await _targetContext.SaveChangesAsync();

                foreach (var sale in sales)
                    existingSaleIds.Add(sale.SaleId);

                LoggerService.RecordTransferCount(sales.Count, nameof(SalesFact));
                processedCount += sales.Count;
            }
        }
    }

    private async Task<Dictionary<long, (int Day, int Month, int Year)>> LoadDateLookupAsync()
    {
        return await _targetContext.DimDates
            .ToDictionaryAsync(d => d.DateId, d => (d.Day, d.Month, d.Year));
    }

    private async Task<Dictionary<long, (bool IsCashless, bool IsReturned)>> LoadTransactionLookupAsync()
    {
        return await _targetContext.TransactionTypes
            .ToDictionaryAsync(t => t.TransactionTypeId, t => (t.IsCashless, t.IsReturned));
    }

    private async Task<List<SalesFact>> GetSalesFactsAsync(
        IQueryable<Sale> salesQuery,
        int skip,
        Dictionary<long, (int Day, int Month, int Year)> dateLookup,
        Dictionary<long, (bool IsCashless, bool IsReturned)> transactionLookup)
    {
        var salesList = await salesQuery
            .AsNoTracking()
            .Include(s => s.DeptCodeNavigation)
            .Include(s => s.Product)
            .OrderBy(s => s.SaleId)
            .Skip(skip)
            .Take(BatchSize)
            .ToListAsync();

        var result = new List<SalesFact>();

        foreach (var s in salesList)
        {
            var saleDate = s.SaleDate;
            var dateId = dateLookup
                .FirstOrDefault(d => d.Value.Day == saleDate.Day &&
                                     d.Value.Month == saleDate.Month &&
                                     d.Value.Year == saleDate.Year).Key;

            if (dateId == 0)
            {
                var newDate = Mapper.MapToDimDate(saleDate);
                _targetContext.DimDates.Add(newDate);
                await _targetContext.SaveChangesAsync();
                dateId = newDate.DateId;
                dateLookup[dateId] = (newDate.Day, newDate.Month, newDate.Year);
            }

            var transactionId = transactionLookup
                .FirstOrDefault(t => t.Value.IsCashless == s.IsCashless &&
                                     t.Value.IsReturned == !string.IsNullOrEmpty(s.ReturnReason)).Key;

            if (transactionId == 0)
                throw new KeyNotFoundException($"No TransactionType found for IsCashless={s.IsCashless}, IsReturned={!string.IsNullOrEmpty(s.ReturnReason)}");

            result.Add(Mapper.MapToSalesFact(
                s.SaleId,
                s,
                dateId,
                s.DeptCode ?? "Unknown",
                s.ProductId,
                transactionId
            ));
        }

        return result;
    }
}
