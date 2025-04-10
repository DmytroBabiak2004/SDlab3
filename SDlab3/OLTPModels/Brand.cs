using System;
using System.Collections.Generic;

namespace SDlab3.OLTPModels;

public partial class Brand
{
    public int BrandId { get; set; }

    public string BrandName { get; set; } = null!;

    public string? Origin { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
