using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class SalesFactTransferService
{
    private readonly StoretransactionsdbContext _sourceContext; // Поле для sourceContext
    private readonly OlapstoreTransactionDbContext _targetContext; // Поле для targetContext
    private const int BatchSize = 1000;

    // Конструктор із залежностями
    public SalesFactTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
    {
        _sourceContext = sourceContext;
        _targetContext = targetContext;
    }

    public async Task TransferSalesFactsAsync()
    {
        var saleCount = await _sourceContext.Sales.CountAsync();
        var processedCount = LoggerService.GetProcessedCount(nameof(SalesFact));

        while (processedCount < saleCount)
        {
            var sales = await GetSalesFacts(_sourceContext.Sales, processedCount);

            await _targetContext.SalesFacts.AddRangeAsync(sales);
            await _targetContext.SaveChangesAsync();

            processedCount += BatchSize;
            LoggerService.RecordTransferCount(processedCount, nameof(SalesFact));
        }
    }

    public async Task TransferNewSalesFactsAsync()
    {
        var existingSaleIds = await _targetContext.SalesFacts
            .Select(s => s.SaleId)
            .ToListAsync();

        while (true)
        {
            var salesToTransfer = await _sourceContext.Sales
                .Where(s => !existingSaleIds.Contains(s.SaleId))
                .OrderBy(s => s.SaleId)
                .Include(s => s.DeptCodeNavigation)
                .Include(s => s.Product)
                .AsNoTracking()
                .Take(BatchSize)
                .Select(s => new Sale
                {
                    SaleId = s.SaleId,
                    ProductId = s.ProductId,
                    DeptCode = s.DeptCode,
                    SaleDate = s.SaleDate,
                    Quantity = s.Quantity,
                    Price = s.Price,
                    IsCashless = s.IsCashless,
                    ReturnReason = s.ReturnReason,
                    DeptCodeNavigation = s.DeptCodeNavigation == null ? null : new Department
                    {
                        DeptCode = s.DeptCodeNavigation.DeptCode,
                        DeptName = s.DeptCodeNavigation.DeptName
                    },
                    Product = s.Product == null ? null : new Product
                    {
                        ProductId = s.Product.ProductId,
                        Name = s.Product.Name
                    }
                })
                .ToListAsync();

            if (salesToTransfer.Count == 0)
                break;

            var newSales = await GetNewSalesFacts(salesToTransfer);

            await _targetContext.SalesFacts.AddRangeAsync(newSales);
            await _targetContext.SaveChangesAsync();

            foreach (var sale in salesToTransfer)
            {
                existingSaleIds.Add(sale.SaleId);
            }
        }
    }

    private async Task<List<SalesFact>> GetSalesFacts(IQueryable<Sale> sales, int skip = 0)
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
                s,
                dateId,
                s.DeptCode ?? "Unknown",
                s.ProductId,
                transactionMatch.Key
            ));
        }

        return result;
    }

    private async Task<List<SalesFact>> GetNewSalesFacts(List<Sale> sales)
    {
        var salesList = sales;

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