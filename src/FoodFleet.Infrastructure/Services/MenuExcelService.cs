using ClosedXML.Excel;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Services;
using FoodFleet.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace FoodFleet.Infrastructure.Services;

public class MenuExcelService(IMenuItemRepository menuRepo, IMenuCategoryRepository categoryRepo, IUnitOfWork uow, ILogger<MenuExcelService> logger)
    : IMenuExcelService
{
    private static readonly string[] ExportHeaders = ["CategoryName", "ItemName", "Description", "Price", "IsAvailable", "StockCount"];

    public Task<byte[]> ExportMenuAsync(Guid branchId, IEnumerable<MenuCategory> categories, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Menu");

        // Header row — styled
        for (int i = 0; i < ExportHeaders.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = ExportHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var category in categories.OrderBy(c => c.DisplayOrder))
        {
            foreach (var item in category.Items.OrderBy(i => i.Name))
            {
                sheet.Cell(row, 1).Value = category.Name;
                sheet.Cell(row, 2).Value = item.Name;
                sheet.Cell(row, 3).Value = item.Description ?? "";
                sheet.Cell(row, 4).Value = item.Price;
                sheet.Cell(row, 5).Value = item.IsAvailable;
                sheet.Cell(row, 6).Value = item.StockCount ?? 0;
                row++;
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    public async Task<ExcelImportResult> ImportMenuAsync(Guid branchId, Stream excelStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(excelStream);
        var sheet = workbook.Worksheet(1);
        var rows = sheet.RowsUsed().Skip(1).ToList();

        var existingCategories = (await categoryRepo.GetByBranchAsync(branchId, ct))
            .ToDictionary(c => c.Name.ToLowerInvariant(), c => c);

        int imported = 0, skipped = 0;
        var errors = new List<ExcelRowError>();

        foreach (var row in rows)
        {
            try
            {
                var categoryName = row.Cell(1).GetString().Trim();
                var itemName = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    skipped++;
                    continue;
                }

                if (!existingCategories.TryGetValue(categoryName.ToLowerInvariant(), out var category))
                {
                    category = MenuCategory.Create(branchId, categoryName);
                    await categoryRepo.AddAsync(category, ct);
                    existingCategories[categoryName.ToLowerInvariant()] = category;
                }

                var description = row.Cell(3).GetString().Trim();
                var price = row.Cell(4).GetValue<decimal>();
                var isAvailable = row.Cell(5).GetValue<bool>();
                var stockCount = row.Cell(6).GetValue<int>();

                var item = MenuItem.Create(category.Id, itemName, price, description.Length > 0 ? description : null);
                item.SetAvailability(isAvailable);
                if (stockCount > 0) item.UpdateStock(stockCount);

                await menuRepo.AddAsync(item, ct);
                imported++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to import menu row {RowNumber}", row.RowNumber());
                errors.Add(new ExcelRowError(row.RowNumber(), ex.Message));
                skipped++;
            }
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Imported {Count} menu items for branch {BranchId}", imported, branchId);
        return new ExcelImportResult(imported, skipped, errors);
    }
}
