using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Pages;

/// <summary>Page Object Model for /dashboard</summary>
public class DashboardPage(IPage page)
{
    private IPage Page { get; } = page;

    public ILocator PageHeading      => Page.Locator("h1", new() { HasTextString = "Dashboard" });
    public ILocator StatCards        => Page.Locator(".grid .rounded-2xl");
    public ILocator SidebarNav       => Page.Locator("nav");
    public ILocator UserMenuButton   => Page.Locator("text=Logout");

    public Task GotoAsync() => Page.GotoAsync("/dashboard");
}
