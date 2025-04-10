using System;
using System.Collections.Generic;

namespace SDlab3.OLAPModels;

public partial class DimDepartment
{
    public string DeptCode { get; set; } = null!;

    public string DeptName { get; set; } = null!;

    public string? City { get; set; }

    public virtual ICollection<MonthlySalesAgg> MonthlySalesAggs { get; set; } = new List<MonthlySalesAgg>();

    public virtual ICollection<SalesFact> SalesFacts { get; set; } = new List<SalesFact>();
}
