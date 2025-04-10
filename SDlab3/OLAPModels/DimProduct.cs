using System;
using System.Collections.Generic;

namespace SDlab3.OLAPModels;

public partial class DimProduct
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? PreviousName1 { get; set; }

    public string? PreviousName2 { get; set; }

    public string? Category { get; set; }

    public string? ParentCategory { get; set; }

    public string? Brand { get; set; }

    public string? Origin { get; set; }

    public virtual ICollection<SalesFact> SalesFacts { get; set; } = new List<SalesFact>();
}
