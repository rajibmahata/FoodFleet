using FoodFleet.Tests.UI.Infrastructure;
using FoodFleet.Tests.UI.Pages;
using FluentAssertions;
using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Tests;

[Collection(nameof(PlaywrightCollection))]
public class OrdersTests(PlaywrightFixture fixture)
{
    private async Task<(IBrowserContext ctx, IPage page)> LoginAndGoToOrdersAsync()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        var login = new LoginPage(page);
        await login.GotoAsync();
        await login.LoginAsync("admin", "admin123");
        // LoginAsync waits until URL leaves /login
        await page.GotoAsync("/orders");
        await page.WaitForURLAsync("**/orders", new() { Timeout = 15_000 });
        return (ctx, page);
    }

    // ── TC-ORD-01 ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Orders_PageLoads_HeadingVisible()
    {
        var (ctx, page) = await LoginAndGoToOrdersAsync();
        try
        {
            var ordersPage = new OrdersPage(page);
            await ordersPage.PageHeading.WaitForAsync();
            var heading = await ordersPage.PageHeading.InnerTextAsync();
            heading.Should().Be("Orders");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-ORD-02 ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Orders_StatusFilter_IsVisible()
    {
        var (ctx, page) = await LoginAndGoToOrdersAsync();
        try
        {
            var ordersPage = new OrdersPage(page);
            await ordersPage.StatusFilter.WaitForAsync();
            var isVisible = await ordersPage.StatusFilter.IsVisibleAsync();
            isVisible.Should().BeTrue();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-ORD-03 ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Orders_PageTitle_IsCorrect()
    {
        var (ctx, page) = await LoginAndGoToOrdersAsync();
        try
        {
            var title = await page.TitleAsync();
            title.Should().Contain("Orders");
        }
        finally { await ctx.DisposeAsync(); }
    }
}
