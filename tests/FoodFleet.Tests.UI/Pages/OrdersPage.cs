using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Pages;

/// <summary>Page Object Model for /orders</summary>
public class OrdersPage(IPage page)
{
    private IPage Page { get; } = page;

    public ILocator PageHeading      => Page.Locator("h1", new() { HasTextString = "Orders" });
    public ILocator StatusFilter     => Page.Locator("select").First;
    public ILocator SearchInput      => Page.Locator("input[placeholder*='Search'], input[type='search']");
    public ILocator OrderRows        => Page.Locator("tbody tr");
    public ILocator ManageButton     => Page.Locator("button", new() { HasTextString = "Manage" }).First;
    public ILocator Modal            => Page.Locator(".fixed.inset-0 .bg-white");
    public ILocator ModalCloseButton => Page.Locator("button[aria-label='Close'], button", new() { HasTextString = "Close" });

    public Task GotoAsync() => Page.GotoAsync("/orders");

    public async Task FilterByStatusAsync(string status)
        => await StatusFilter.SelectOptionAsync(new SelectOptionValue { Label = status });
}
