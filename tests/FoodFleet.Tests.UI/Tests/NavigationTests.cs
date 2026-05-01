using FoodFleet.Tests.UI.Infrastructure;
using FoodFleet.Tests.UI.Pages;
using FluentAssertions;
using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Tests;

[Collection(nameof(PlaywrightCollection))]
public class NavigationTests(PlaywrightFixture fixture)
{
    private async Task<(IBrowserContext ctx, IPage page)> LoginAsync()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        var login = new LoginPage(page);
        await login.GotoAsync();
        await login.LoginAsync("admin", "admin123");
        // LoginAsync waits until URL leaves /login
        return (ctx, page);
    }

    // ── TC-NAV-01 ─────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("/dashboard", "Dashboard")]
    [InlineData("/branches",  "Branches")]
    [InlineData("/menu",      "Menu")]
    [InlineData("/orders",    "Orders")]
    [InlineData("/settings",  "Settings")]
    public async Task SidebarNavigation_AllRoutes_LoadCorrectly(string route, string expectedHeading)
    {
        var (ctx, page) = await LoginAsync();
        try
        {
            await page.GotoAsync(route);
            await page.WaitForURLAsync($"**{route}");

            var heading = page.Locator("h1").First;
            await heading.WaitForAsync();
            var text = await heading.InnerTextAsync();
            text.Should().Contain(expectedHeading);
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-NAV-02 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Logout_NavigatesToLoginPage()
    {
        var (ctx, page) = await LoginAsync();
        try
        {
            // Click logout link in sidebar
            var logoutLink = page.Locator("a[href='/logout'], nav a", new() { HasTextString = "Logout" });
            await logoutLink.WaitForAsync();
            await logoutLink.ClickAsync();

            await page.WaitForURLAsync("**/login", new() { Timeout = 8_000 });
            page.Url.Should().Contain("/login");
        }
        finally { await ctx.DisposeAsync(); }
    }
}
