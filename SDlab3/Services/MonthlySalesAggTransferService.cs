using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class MonthlySalesAggTransferService
{
    private readonly OlapstoreTransactionDbContext _targetContext;
    private const int BatchSize = 1000;

    public MonthlySalesAggTransferService(OlapstoreTransactionDbContext targetContext)
    {
        _targetContext = targetContext;
    }

    public async Task TransferMonthlySalesAggAsync()
    {
        int processedCount = LoggerService.GetProcessedCount(nameof(MonthlySalesAgg));
        var totalSalesFacts = await _targetContext.SalesFacts.CountAsync();

        while (processedCount < totalSalesFacts)
        {
            var salesBatch = await _targetContext.SalesFacts
                .Skip(processedCount)
                .Take(BatchSize)
                .Join(_targetContext.DimDates,
                    sf => sf.DateId,
                    dd => dd.DateId,
                    (sf, dd) => new { SalesFact = sf, DimDate = dd })
                .Join(_targetContext.TransactionTypes,
                    s => s.SalesFact.TransactionTypeId,
                    tt => tt.TransactionTypeId,
                    (s, tt) => new { s.SalesFact, s.DimDate, TransactionType = tt })
                .ToListAsync();

            if (salesBatch.Count == 0)
                break;

            var monthLookup = await _targetContext.DimMonths
                .ToDictionaryAsync(m => new { m.Month, m.Year }, m => m.MonthId);

            var salesByMonthAndDept = salesBatch
                .GroupBy(s => new { s.DimDate.Month, s.DimDate.Year, s.SalesFact.DeptCode })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    DeptCode = g.Key.DeptCode,
                    TotalSales = g.Sum(s => s.SalesFact.TotalSales * s.SalesFact.Quantity), // Врахування кількості
                    TotalQuantity = g.Sum(s => s.SalesFact.Quantity),
                    AverageDiscount = g.Any(s => s.SalesFact.Discount != 0) ? g.Average(s => s.SalesFact.Discount) : 0m,
                    TotalReturns = g.Count(s => s.TransactionType.IsReturned),
                    PercentCashless = g.Count() > 0 ? g.Count(s => s.TransactionType.IsCashless) * 100m / g.Count() : 0m
                });

            foreach (var agg in salesByMonthAndDept)
            {
                var monthId = monthLookup[new { agg.Month, agg.Year }];
                var existingAgg = await _targetContext.MonthlySalesAggs
                    .FirstOrDefaultAsync(m => m.MonthId == monthId && m.DeptCode == agg.DeptCode);

                if (existingAgg == null)
                {
                    var newAgg = Mapper.MapToMonthlySalesAgg(
                        monthId,
                        agg.DeptCode,
                        agg.TotalSales,
                        agg.TotalQuantity,
                        agg.AverageDiscount,
                        agg.TotalReturns,
                        agg.PercentCashless
                    );
                    _targetContext.MonthlySalesAggs.Add(newAgg);
                }
                else
                {
                    existingAgg.TotalSales = agg.TotalSales; // Повна заміна, щоб уникнути дублювання
                    existingAgg.TotalQuantity = agg.TotalQuantity;
                    existingAgg.AverageDiscount = agg.AverageDiscount;
                    existingAgg.TotalReturns = agg.TotalReturns;
                    existingAgg.PercentCashless = agg.PercentCashless;
                }
            }

            await _targetContext.SaveChangesAsync();
            processedCount += salesBatch.Count;
            LoggerService.RecordTransferCount(processedCount, nameof(MonthlySalesAgg));
        }
    }
}