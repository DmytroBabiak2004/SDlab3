using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SDlab3.OLAPModels;
using SDlab3.Models;

namespace SDlab3.Services
{
    public class MonthlySalesAggService
    {
        private readonly OlapstoreTransactionDbContext _context; 

        public MonthlySalesAggService(OlapstoreTransactionDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task GenerateMonthlySalesAggregatesAsync()
        {
           var aggregates = await _context.SalesFacts
                .Join(_context.TransactionTypes,
                    sf => sf.TransactionTypeId,
                    tt => tt.TransactionTypeId,
                    (sf, tt) => new { SalesFact = sf, TransactionType = tt })
                .Join(_context.DimDates,
                    sf => sf.SalesFact.DateId,
                    dd => dd.DateId,
                    (sf, dd) => new { sf.SalesFact, sf.TransactionType, DimDate = dd })
                .Join(_context.DimMonths,
                    sf => new { sf.DimDate.Month, sf.DimDate.Year },
                    dm => new { dm.Month, dm.Year },
                    (sf, dm) => new { sf.SalesFact, sf.TransactionType, DimMonth = dm })
                .GroupBy(x => new { x.DimMonth.MonthId, x.SalesFact.DeptCode })
                .Select(g => new
                {
                    MonthId = g.Key.MonthId,
                    DeptCode = g.Key.DeptCode,
                    TotalSales = g.Sum(x => x.SalesFact.TotalSales),
                    TotalQuantity = g.Sum(x => x.SalesFact.Quantity),
                    AverageDiscount = g.Average(x => x.SalesFact.Discount),
                    TotalReturns = g.Count(x => x.TransactionType.IsReturned),
                    PercentCashless = g.Average(x => x.TransactionType.IsCashless ? 1.0m : 0.0m) * 100
                })
                .ToListAsync();

            _context.MonthlySalesAggs.RemoveRange(_context.MonthlySalesAggs);
            await _context.SaveChangesAsync();

            foreach (var agg in aggregates)
            {
                var monthlySalesAgg = Mapper.MapToMonthlySalesAgg(
                    monthId: agg.MonthId,
                    deptCode: agg.DeptCode,
                    totalSales: agg.TotalSales,
                    totalQuantity: agg.TotalQuantity,
                    averageDiscount: agg.AverageDiscount,
                    totalReturns: agg.TotalReturns,
                    percentCashless: agg.PercentCashless
                );

                _context.MonthlySalesAggs.Add(monthlySalesAgg);
            }

            await _context.SaveChangesAsync();
        }
    }
}