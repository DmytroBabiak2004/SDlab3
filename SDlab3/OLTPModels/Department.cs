using System;
using System.Collections.Generic;

namespace SDlab3.OLTPModels;

public partial class Department
{
    public string DeptCode { get; set; } = null!;

    public string DeptName { get; set; } = null!;

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
