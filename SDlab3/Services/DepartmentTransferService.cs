using SDlab3.OLTPModels;
using SDlab3.OLAPModels;
using SDlab3.Models;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.Services;

public class DepartmentTransferService(StoretransactionsdbContext sourceContext, OlapstoreTransactionDbContext targetContext)
{
	private const int BatchSize = 1000;

	public async Task TransferDepartmentsAsync()
	{
		var deptCount = await sourceContext.Departments.CountAsync();
		var processedCount = LoggerService.GetProcessedCount(nameof(DimDepartment));

		while (processedCount < deptCount)
		{
			var departments = await GetDepartments(sourceContext.Departments, processedCount);

			await targetContext.DimDepartments.AddRangeAsync(departments);
			await targetContext.SaveChangesAsync();

			processedCount += BatchSize;
			LoggerService.RecordTransferCount(processedCount, nameof(DimDepartment));
		}
	}

	public async Task TransferNewDepartmentsAsync()
	{
		var existingDeptCodes = await targetContext.DimDepartments
			.Select(d => d.DeptCode)
			.ToListAsync();

		while (true)
		{
			var newDepartments = await GetDepartments(
				sourceContext.Departments.Where(d => !existingDeptCodes.Contains(d.DeptCode))
			);

			if (newDepartments.Count == 0)
				break;

			await targetContext.DimDepartments.AddRangeAsync(newDepartments);
			await targetContext.SaveChangesAsync();

			foreach (var dept in newDepartments)
			{
				existingDeptCodes.Add(dept.DeptCode);
			}
		}
	}

	private static async Task<List<DimDepartment>> GetDepartments(IQueryable<Department> departments, int skip = 0)
	{
		return await departments
			.AsNoTracking()
			.OrderBy(d => d.DeptCode)
			.Skip(skip)
			.Take(BatchSize)
			.Select(d => Mapper.MapToDimDepartment(d))
			.ToListAsync();
	}
}