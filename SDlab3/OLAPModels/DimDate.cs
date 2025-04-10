using System;
using System.Collections.Generic;

namespace SDlab3.OLAPModels;

public partial class DimDate
{
    public long DateId { get; set; }

    public int Day { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public int Month { get; set; }

    public int Year { get; set; }

    public virtual ICollection<SalesFact> SalesFacts { get; set; } = new List<SalesFact>();
}
