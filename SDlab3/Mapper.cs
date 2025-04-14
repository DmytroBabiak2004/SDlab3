using SDlab3.OLTPModels;
using SDlab3.OLAPModels;

namespace SDlab3.Models
{
    public class Mapper
    {
        // 1. DimDate
        public static DimDate MapToDimDate(DateTime saleDate)
        {
            return new DimDate
            {
                Day = saleDate.Day,
                DayOfWeek = saleDate.ToString("dddd"), 
                Month = saleDate.Month,
                Year = saleDate.Year
            };
        }

        // 2. DimMonth
        public static DimMonth MapToDimMonth(DateTime saleDate)
        {
            return new DimMonth
            {
                Month = saleDate.Month,
                Year = saleDate.Year
            };
        }

        // 3. DimProduct
        public static DimProduct MapToDimProduct(Product product, ProductCategory category, Brand brand)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            return new DimProduct
            {
                ProductName = product.Name,
                Category = category?.Name,
                ParentCategory = category?.ParentCategoryId.HasValue == true ? category.ParentCategory?.Name : null,
                Brand = brand?.BrandName,
                Origin = brand?.Origin,
                ProductId = (int)product.ProductId
            };
        }

        // 4. DimDepartment
        public static DimDepartment MapToDimDepartment(Department department)
        {
            return new DimDepartment
            {
                DeptCode = department.DeptCode,
                DeptName = department.DeptName,
                City = "Unknown" 
            };
        }

        // 5. TransactionType
        public static TransactionType MapToTransactionType(Sale sale)
        {
            return new TransactionType
            {
                IsCashless = sale.IsCashless, 
                IsReturned = !string.IsNullOrEmpty(sale.ReturnReason)
            };
        }

        // 6. SalesFact
        public static SalesFact MapToSalesFact(
            long saleId,
            Sale sale,
            long dateId,
            string deptCode,
            long productId,
            long transactionTypeId)
        {
            return new SalesFact
            {
                SaleId = saleId,
                DateId = dateId,
                DeptCode = deptCode,
                ProductId = productId,
                TotalSales = sale.Price,
                Quantity = sale.Quantity, 
                Discount = sale.Discount ?? 0m, 
                TransactionTypeId = transactionTypeId
            };
        }

        // 7. MonthlySalesAgg
        public static MonthlySalesAgg MapToMonthlySalesAgg(
            int monthId,
            string deptCode,
            decimal totalSales,
            int totalQuantity,
            decimal? averageDiscount,
            int totalReturns,
            decimal percentCashless)
        {
            return new MonthlySalesAgg
            {
                MonthId = monthId,
                DeptCode = deptCode,
                TotalSales = totalSales,
                TotalQuantity = totalQuantity,
                AverageDiscount = averageDiscount,
                TotalReturns = totalReturns,
                PercentCashless = percentCashless
            };
        }
    }
}