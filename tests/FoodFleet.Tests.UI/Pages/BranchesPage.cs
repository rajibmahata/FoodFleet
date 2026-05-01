using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Pages;

/// <summary>Page Object Model for /branches</summary>
public class BranchesPage(IPage page)
{
    private IPage Page { get; } = page;

    public ILocator PageHeading      => Page.Locator("h1", new() { HasTextString = "Branches" });
    public ILocator AddBranchButton  => Page.Locator("button", new() { HasTextString = "Add Branch" });
    public ILocator BranchRows       => Page.Locator("tbody tr");
    public ILocator Modal            => Page.Locator("[role='dialog'], .fixed.inset-0 .bg-white");

    // Modal fields
    public ILocator NameInput        => Page.Locator("input[placeholder*='Name'], input#name");
    public ILocator AddressInput     => Page.Locator("input[placeholder*='Address']");
    public ILocator CityInput        => Page.Locator("input[placeholder*='City']");
    public ILocator PhoneInput       => Page.Locator("input[placeholder*='Phone']");
    public ILocator SaveButton       => Page.Locator("button[type='submit']");
    public ILocator CancelButton     => Page.Locator("button", new() { HasTextString = "Cancel" });

    public Task GotoAsync() => Page.GotoAsync("/branches");
}
