using FoodFleet.Domain.Entities;

namespace FoodFleet.Domain.Interfaces.Services;

public record ExcelImportResult(int Imported, int Skipped, List<ExcelRowError> Errors);
public record ExcelRowError(int Row, string Reason);

public interface IMenuExcelService
{
    Task<byte[]> ExportMenuAsync(Guid branchId, IEnumerable<MenuCategory> categories, CancellationToken ct = default);
    Task<ExcelImportResult> ImportMenuAsync(Guid branchId, Stream excelStream, CancellationToken ct = default);
}
