using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin menu management — categories, items, stock, availability, bulk import/export.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/menu")]
[Produces("application/json")]
public class AdminMenuController : ApiControllerBase
{
    // --- Categories ---

    /// <summary>List all menu categories for a branch.</summary>
    [HttpGet("categories/{branchId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<MenuCategoryDto>), 200)]
    public async Task<IActionResult> GetCategories(Guid branchId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetMenuCategoriesQuery(branchId), ct));

    /// <summary>Create a new menu category.</summary>
    /// <response code="201">Category created.</response>
    [HttpPost("categories")]
    [ProducesResponseType(typeof(MenuCategoryDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new CreateMenuCategoryCommand(request), ct));

    /// <summary>Update a menu category's name or sort order.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{categoryId:guid}")]
    [ProducesResponseType(typeof(MenuCategoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, [FromBody] UpdateMenuCategoryRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateMenuCategoryCommand(categoryId, request), ct));

    /// <summary>Delete a menu category and all its items.</summary>
    /// <response code="200">Category deleted.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("categories/{categoryId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCategory(Guid categoryId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new DeleteMenuCategoryCommand(categoryId), ct));

    // --- Items ---

    /// <summary>List menu items for a category.</summary>
    /// <param name="categoryId">The category to filter by.</param>
    [HttpGet("items")]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), 200)]
    public async Task<IActionResult> GetItems([FromQuery] Guid categoryId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetMenuItemsByCategoryQuery(categoryId), ct));

    /// <summary>Create a new menu item.</summary>
    /// <response code="201">Item created.</response>
    [HttpPost("items")]
    [ProducesResponseType(typeof(MenuItemDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateItem([FromBody] CreateMenuItemRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new CreateMenuItemCommand(request), ct));

    /// <summary>Update a menu item's details (name, price, description, image).</summary>
    /// <response code="200">Item updated.</response>
    /// <response code="404">Item not found.</response>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(MenuItemDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateMenuItemRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateMenuItemCommand(itemId, request), ct));

    /// <summary>Delete a menu item.</summary>
    /// <response code="200">Item deleted.</response>
    /// <response code="404">Item not found.</response>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteItem(Guid itemId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new DeleteMenuItemCommand(itemId), ct));

    /// <summary>Update the stock count for a menu item.</summary>
    [HttpPatch("items/{itemId:guid}/stock")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStock(Guid itemId, [FromBody] UpdateStockRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new UpdateStockCommand(itemId, request.StockCount), ct));

    /// <summary>Toggle whether a menu item is currently available for ordering.</summary>
    [HttpPatch("items/{itemId:guid}/availability")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleAvailability(Guid itemId, [FromBody] UpdateAvailabilityRequest request, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new ToggleAvailabilityCommand(itemId, request.IsAvailable), ct));

    // --- Excel Import/Export ---

    /// <summary>Export a branch's entire menu as an Excel (.xlsx) file.</summary>
    /// <response code="200">Excel file returned as <c>application/vnd.openxmlformats-officedocument.spreadsheetml.sheet</c>.</response>
    [HttpGet("export/{branchId:guid}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Export(Guid branchId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new ExportMenuExcelQuery(branchId), ct));

    /// <summary>Import menu items from an Excel (.xlsx) file (bulk upsert by name).</summary>
    /// <response code="200">Import completed. Response contains row counts.</response>
    /// <response code="400">File is empty or malformed.</response>
    [HttpPost("import/{branchId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Import(Guid branchId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(new { message = "File is empty." });
        using var stream = file.OpenReadStream();
        return ToActionResult(await Mediator.Send(new ImportMenuExcelCommand(branchId, stream), ct));
    }
}
