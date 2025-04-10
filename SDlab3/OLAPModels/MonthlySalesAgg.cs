using System;
using System.Collections.Generic;

namespace SDlab3.OLAPModels;

public partial class MonthlySalesAgg
{
    public int MonthId { get; set; }

    public string DeptCode { get; set; } = null!;

    public decimal TotalSales { get; set; }

    public int TotalQuantity { get; set; }

    public decimal? AverageDiscount { get; set; }

    public int? TotalReturns { get; set; }

    public decimal? PercentCashless { get; set; }

    public virtual DimDepartment DeptCodeNavigation { get; set; } = null!;

    public virtual DimMonth Month { get; set; } = null!;
}
