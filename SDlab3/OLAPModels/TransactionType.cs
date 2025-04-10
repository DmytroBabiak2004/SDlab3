using System;
using System.Collections.Generic;

namespace SDlab3.OLAPModels;

public partial class TransactionType
{
    public long TransactionTypeId { get; set; }

    public bool IsCashless { get; set; }

    public bool IsReturned { get; set; }

    public virtual ICollection<SalesFact> SalesFacts { get; set; } = new List<SalesFact>();
}
