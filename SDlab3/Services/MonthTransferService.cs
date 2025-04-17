using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class MonthTransferService
{
    private readonly StoretransactionsdbContext _sourceContext;
    private readonly OlapstoreTransactionDbContext _targetContext;

    public MonthTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
    {
        _sourceContext = sourceContext;
        _targetContext = targetContext;
    }

    public async Task TransferDimMonthsAsync()
    {
        var uniqueMonths = await _sourceContext.Sales
            .Select(s => new { s.SaleDate.Month, s.SaleDate.Year })
            .Distinct()
            .ToListAsync();

       var existingMonths = await _targetContext.DimMonths
            .ToDictionaryAsync(m => new { m.Month, m.Year }, m => m.MonthId);

        foreach (var month in uniqueMonths)
        {
            if (!existingMonths.ContainsKey(new { month.Month, month.Year }))
            {
                var newDimMonth = Mapper.MapToDimMonth(new DateTime(month.Year, month.Month, 1));
                _targetContext.DimMonths.Add(newDimMonth);
                existingMonths[new { month.Month, month.Year }] = newDimMonth.MonthId;
            }
        }

        await _targetContext.SaveChangesAsync();
    }
}