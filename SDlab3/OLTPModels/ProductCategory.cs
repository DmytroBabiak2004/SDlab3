using System;
using System.Collections.Generic;

namespace SDlab3.OLTPModels;

public partial class ProductCategory
{
    public long CategoryId { get; set; }

    public long? ParentCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public bool? IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProductCategory> InverseParentCategory { get; set; } = new List<ProductCategory>();

    public virtual ProductCategory? ParentCategory { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
