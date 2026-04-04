using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using MediatR;

namespace FoodFleet.Application.Features.Menu;

// ── Category ─────────────────────────────────────────────────
public record GetMenuCategoriesQuery(Guid BranchId) : IRequest<Result<IEnumerable<MenuCategoryDto>>>;
public record GetMenuItemsByCategoryQuery(Guid CategoryId) : IRequest<Result<IEnumerable<MenuItemDto>>>;
public record CreateMenuCategoryCommand(CreateMenuCategoryRequest Request) : IRequest<Result<MenuCategoryDto>>;
public record UpdateMenuCategoryCommand(Guid Id, UpdateMenuCategoryRequest Request) : IRequest<Result<MenuCategoryDto>>;
public record DeleteMenuCategoryCommand(Guid Id) : IRequest<Result>;

// ── Item ─────────────────────────────────────────────────────
public record CreateMenuItemCommand(CreateMenuItemRequest Request) : IRequest<Result<MenuItemDto>>;
public record UpdateMenuItemCommand(Guid Id, UpdateMenuItemRequest Request) : IRequest<Result<MenuItemDto>>;
public record DeleteMenuItemCommand(Guid Id) : IRequest<Result>;
public record UpdateStockCommand(Guid Id, int? StockCount) : IRequest<Result<MenuItemDto>>;
public record ToggleAvailabilityCommand(Guid Id, bool IsAvailable) : IRequest<Result<MenuItemDto>>;
public record CreateVariantCommand(CreateMenuItemVariantRequest Request) : IRequest<Result<MenuItemVariantDto>>;
public record DeleteVariantCommand(Guid Id) : IRequest<Result>;

// ── Handlers ─────────────────────────────────────────────────
public class GetMenuCategoriesQueryHandler(IUnitOfWork uow) : IRequestHandler<GetMenuCategoriesQuery, Result<IEnumerable<MenuCategoryDto>>>
{
    public async Task<Result<IEnumerable<MenuCategoryDto>>> Handle(GetMenuCategoriesQuery query, CancellationToken ct)
    {
        var categories = await uow.MenuCategories.GetByBranchAsync(query.BranchId, ct);
        return Result<IEnumerable<MenuCategoryDto>>.Success(categories.Select(MenuMapper.MapCategoryDto));
    }
}

public class GetMenuItemsByCategoryQueryHandler(IUnitOfWork uow) : IRequestHandler<GetMenuItemsByCategoryQuery, Result<IEnumerable<MenuItemDto>>>
{
    public async Task<Result<IEnumerable<MenuItemDto>>> Handle(GetMenuItemsByCategoryQuery query, CancellationToken ct)
    {
        var items = await uow.MenuItems.GetByCategoryAsync(query.CategoryId, ct);
        return Result<IEnumerable<MenuItemDto>>.Success(items.Select(MenuMapper.MapItemDto));
    }
}

public class CreateMenuCategoryCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateMenuCategoryCommand, Result<MenuCategoryDto>>
{
    public async Task<Result<MenuCategoryDto>> Handle(CreateMenuCategoryCommand cmd, CancellationToken ct)
    {
        var category = MenuCategory.Create(cmd.Request.BranchId, cmd.Request.Name, cmd.Request.DisplayOrder);
        await uow.MenuCategories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
        return Result<MenuCategoryDto>.Success(MenuMapper.MapCategoryDto(category), 201);
    }
}

public class UpdateMenuCategoryCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateMenuCategoryCommand, Result<MenuCategoryDto>>
{
    public async Task<Result<MenuCategoryDto>> Handle(UpdateMenuCategoryCommand cmd, CancellationToken ct)
    {
        var category = await uow.MenuCategories.GetByIdAsync(cmd.Id, ct);
        if (category is null) return Result<MenuCategoryDto>.NotFound();
        category.Update(cmd.Request.Name, cmd.Request.DisplayOrder);
        uow.MenuCategories.Update(category);
        await uow.SaveChangesAsync(ct);
        return Result<MenuCategoryDto>.Success(MenuMapper.MapCategoryDto(category));
    }
}

public class DeleteMenuCategoryCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteMenuCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteMenuCategoryCommand cmd, CancellationToken ct)
    {
        var category = await uow.MenuCategories.GetByIdAsync(cmd.Id, ct);
        if (category is null) return Result.NotFound();
        category.Deactivate();
        uow.MenuCategories.Update(category);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class CreateMenuItemCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateMenuItemCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(CreateMenuItemCommand cmd, CancellationToken ct)
    {
        var item = MenuItem.Create(cmd.Request.CategoryId, cmd.Request.Name, cmd.Request.Price, cmd.Request.Description, cmd.Request.ImageUrl, cmd.Request.StockCount);
        await uow.MenuItems.AddAsync(item, ct);
        await uow.SaveChangesAsync(ct);
        return Result<MenuItemDto>.Success(MenuMapper.MapItemDto(item), 201);
    }
}

public class UpdateMenuItemCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateMenuItemCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(UpdateMenuItemCommand cmd, CancellationToken ct)
    {
        var item = await uow.MenuItems.GetByIdAsync(cmd.Id, ct);
        if (item is null) return Result<MenuItemDto>.NotFound();
        item.Update(cmd.Request.Name, cmd.Request.Description, cmd.Request.Price, cmd.Request.ImageUrl);
        uow.MenuItems.Update(item);
        await uow.SaveChangesAsync(ct);
        return Result<MenuItemDto>.Success(MenuMapper.MapItemDto(item));
    }
}

public class DeleteMenuItemCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteMenuItemCommand, Result>
{
    public async Task<Result> Handle(DeleteMenuItemCommand cmd, CancellationToken ct)
    {
        var item = await uow.MenuItems.GetByIdAsync(cmd.Id, ct);
        if (item is null) return Result.NotFound();
        uow.MenuItems.Remove(item);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public class UpdateStockCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateStockCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(UpdateStockCommand cmd, CancellationToken ct)
    {
        var item = await uow.MenuItems.GetByIdAsync(cmd.Id, ct);
        if (item is null) return Result<MenuItemDto>.NotFound();
        item.UpdateStock(cmd.StockCount);
        uow.MenuItems.Update(item);
        await uow.SaveChangesAsync(ct);
        return Result<MenuItemDto>.Success(MenuMapper.MapItemDto(item));
    }
}

public class ToggleAvailabilityCommandHandler(IUnitOfWork uow) : IRequestHandler<ToggleAvailabilityCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(ToggleAvailabilityCommand cmd, CancellationToken ct)
    {
        var item = await uow.MenuItems.GetByIdAsync(cmd.Id, ct);
        if (item is null) return Result<MenuItemDto>.NotFound();
        item.SetAvailability(cmd.IsAvailable);
        uow.MenuItems.Update(item);
        await uow.SaveChangesAsync(ct);
        return Result<MenuItemDto>.Success(MenuMapper.MapItemDto(item));
    }
}

internal static class MenuMapper
{
    internal static MenuCategoryDto MapCategoryDto(MenuCategory c) => new(
        c.Id, c.BranchId, c.Name, c.DisplayOrder, c.IsActive,
        c.Items.Select(MapItemDto)
    );

    internal static MenuItemDto MapItemDto(MenuItem i) => new(
        i.Id, i.CategoryId, i.Name, i.Description, i.Price,
        i.ImageUrl, i.IsAvailable, i.StockCount,
        i.Variants.Select(v => new MenuItemVariantDto(v.Id, v.Label, v.AdditionalPrice))
    );
}

// -- Excel Export --------------------------------------------------------------
public record ExportMenuExcelQuery(Guid BranchId) : IRequest<Result<byte[]>>;

public class ExportMenuExcelQueryHandler(IUnitOfWork uow, IMenuExcelService excelService)
    : IRequestHandler<ExportMenuExcelQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(ExportMenuExcelQuery query, CancellationToken ct)
    {
        var categories = await uow.MenuCategories.GetByBranchAsync(query.BranchId, ct);
        var bytes = await excelService.ExportMenuAsync(query.BranchId, categories, ct);
        return Result<byte[]>.Success(bytes);
    }
}

// -- Excel Import --------------------------------------------------------------
public record ImportMenuExcelCommand(Guid BranchId, Stream ExcelStream) : IRequest<Result<ExcelImportResultDto>>;

public class ImportMenuExcelCommandHandler(IMenuExcelService excelService)
    : IRequestHandler<ImportMenuExcelCommand, Result<ExcelImportResultDto>>
{
    public async Task<Result<ExcelImportResultDto>> Handle(ImportMenuExcelCommand cmd, CancellationToken ct)
    {
        var result = await excelService.ImportMenuAsync(cmd.BranchId, cmd.ExcelStream, ct);
        return Result<ExcelImportResultDto>.Success(new ExcelImportResultDto(result.Imported, result.Skipped, result.Errors.Select(e => $"Row {e.Row}: {e.Reason}").ToList()));
    }
}
