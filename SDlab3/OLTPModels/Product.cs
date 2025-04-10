using System;
using System.Collections.Generic;

namespace SDlab3.OLTPModels;

public partial class Product
{
    public long ProductId { get; set; }

    public long? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public string Name { get; set; } = null!;

    public decimal? LastSupplyPrice { get; set; }

    public decimal? LastStockPrice { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ProductCategory? Category { get; set; }

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
