using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Services;
using Microsoft.EntityFrameworkCore;

namespace SDlab3
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Жорстко закодовані рядки підключення до баз даних
            var sourceConnectionString = "Host=localhost;Port=5432;Database=storetransactionsdb;Username=postgres;Password=admin;CommandTimeout=60";
            var targetConnectionString = "Host=localhost;Port=5432;Database=OLAPStoreTransactionDB;Username=postgres;Password=admin;CommandTimeout=60";

            // Налаштування контекстів баз даних
            var sourceOptions = new DbContextOptionsBuilder<StoretransactionsdbContext>()
                .UseNpgsql(sourceConnectionString)
                .Options;
            var targetOptions = new DbContextOptionsBuilder<OlapstoreTransactionDbContext>()
                .UseNpgsql(targetConnectionString)
                .Options;

            // Ініціалізація контекстів
            using var sourceContext = new StoretransactionsdbContext(sourceOptions);
            using var targetContext = new OlapstoreTransactionDbContext(targetOptions);

            // Ініціалізація сервісів
            var dateService = new DateTransferService(sourceContext, targetContext);
            var deptService = new DepartmentTransferService(sourceContext, targetContext);
            var productService = new ProductTransferService(sourceContext, targetContext);
            var transactionService = new TransactionTypeTransferService(sourceContext, targetContext);
            var salesService = new SalesFactTransferService(sourceContext, targetContext);
            var dimMonthService = new MonthTransferService(sourceContext, targetContext);
            var monthlySalesAggService = new MonthlySalesAggTransferService(targetContext);

            // Виконання первинного завантаження
            Console.WriteLine("Starting initial data transfer...");
            await dateService.TransferDatesAsync();
            await deptService.TransferDepartmentsAsync();
            await productService.TransferProductsAsync();
            await transactionService.TransferTransactionTypesAsync();
            await salesService.TransferSalesFactsAsync();
            await dimMonthService.TransferDimMonthsAsync();
            await monthlySalesAggService.TransferMonthlySalesAggAsync();

            //// Виконання інкрементного оновлення
            //Console.WriteLine("Starting incremental update...");
            //await dateService.TransferNewDatesAsync();
            //await deptService.TransferNewDepartmentsAsync();
            //await productService.TransferNewProductsAsync();
            //await transactionService.TransferNewTransactionTypesAsync();
            //await salesService.TransferNewSalesFactsAsync();

            Console.WriteLine("Data transfer completed.");
        }
    }
}