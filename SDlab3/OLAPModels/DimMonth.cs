using System;
using System.Collections.Generic;

namespace SDlab3.OLAPModels;

public partial class DimMonth
{
    public int MonthId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public virtual ICollection<MonthlySalesAgg> MonthlySalesAggs { get; set; } = new List<MonthlySalesAgg>();
}
