using System;
using System.Collections.Generic;

namespace SDlab3.OLTPModels;

public partial class Sale
{
    public long SaleId { get; set; }

    public long ProductId { get; set; }

    public bool IsCashless { get; set; }

    public DateTime SaleDate { get; set; }

    public string TransactionCode { get; set; } = null!;

    public string EmployeeCode { get; set; } = null!;

    public string DeptCode { get; set; } = null!;

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public decimal? Discount { get; set; }

    public string? ReturnReason { get; set; }

    public virtual Department DeptCodeNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
