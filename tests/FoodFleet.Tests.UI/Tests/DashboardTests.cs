using FoodFleet.Tests.UI.Infrastructure;
using FoodFleet.Tests.UI.Pages;
using FluentAssertions;
using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Tests;

[Collection(nameof(PlaywrightCollection))]
public class DashboardTests(PlaywrightFixture fixture)
{
    private async Task<(Microsoft.Playwright.IBrowserContext ctx, IPage page)> LoginAndGetPageAsync()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        var login = new LoginPage(page);
        await login.GotoAsync();
        await login.LoginAsync("admin", "admin123");
        // LoginAsync waits until URL leaves /login
        return (ctx, page);
    }

    // ── TC-DASH-01 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Dashboard_Loads_PageHeadingVisible()
    {
        var (ctx, page) = await LoginAndGetPageAsync();
        try
        {
            var dashboard = new DashboardPage(page);
            await dashboard.PageHeading.WaitForAsync();
            var heading = await dashboard.PageHeading.InnerTextAsync();
            heading.Should().Be("Dashboard");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-DASH-02 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Dashboard_SidebarNavigation_ContainsExpectedLinks()
    {
        var (ctx, page) = await LoginAndGetPageAsync();
        try
        {
            var sidebar = page.Locator("nav");
            await sidebar.WaitForAsync();
            var navText = await sidebar.InnerTextAsync();

            navText.Should().ContainAny("Dashboard", "Branches", "Menu", "Orders", "Delivery", "Settings");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-DASH-03 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Dashboard_StatCards_AreVisible()
    {
        var (ctx, page) = await LoginAndGetPageAsync();
        try
        {
            var dashboard = new DashboardPage(page);
            // Wait for at least one stat card to be present
            await page.Locator(".rounded-2xl").First.WaitForAsync();
            var cardCount = await dashboard.StatCards.CountAsync();
            cardCount.Should().BeGreaterThan(0);
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-DASH-04 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Dashboard_SidebarBranchesLink_NavigatesToBranchesPage()
    {
        var (ctx, page) = await LoginAndGetPageAsync();
        try
        {
            await page.Locator("nav a", new() { HasTextString = "Branches" }).ClickAsync();
            await page.WaitForURLAsync("**/branches");
            page.Url.Should().Contain("/branches");
        }
        finally { await ctx.DisposeAsync(); }
    }
}
