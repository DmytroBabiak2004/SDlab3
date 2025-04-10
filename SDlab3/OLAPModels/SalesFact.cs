using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SDlab3.OLAPModels;

public partial class SalesFact
{
    public long SaleId { get; set; }

    public long? DateId { get; set; }

    public string? DeptCode { get; set; }

    public long ProductId { get; set; }

    public decimal TotalSales { get; set; }

    public int Quantity { get; set; }

    public decimal? Discount { get; set; }

    public long? TransactionTypeId { get; set; }

    public virtual DimDate? Date { get; set; }

    public virtual DimDepartment? DeptCodeNavigation { get; set; }

    public virtual DimProduct? Product { get; set; }

    public virtual TransactionType? TransactionType { get; set; }
}
