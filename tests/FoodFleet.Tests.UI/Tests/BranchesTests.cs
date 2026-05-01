using FoodFleet.Tests.UI.Infrastructure;
using FoodFleet.Tests.UI.Pages;
using FluentAssertions;
using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Tests;

[Collection(nameof(PlaywrightCollection))]
public class BranchesTests(PlaywrightFixture fixture)
{
    private async Task<(IBrowserContext ctx, IPage page)> LoginAndGoToBranchesAsync()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        var login = new LoginPage(page);
        await login.GotoAsync();
        await login.LoginAsync("admin", "admin123");
        // LoginAsync waits until URL leaves /login
        await page.GotoAsync("/branches");
        await page.WaitForURLAsync("**/branches", new() { Timeout = 15_000 });
        return (ctx, page);
    }

    // ── TC-BR-01 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Branches_PageLoads_HeadingVisible()
    {
        var (ctx, page) = await LoginAndGoToBranchesAsync();
        try
        {
            var branchPage = new BranchesPage(page);
            await branchPage.PageHeading.WaitForAsync();
            var heading = await branchPage.PageHeading.InnerTextAsync();
            heading.Should().Be("Branches");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-BR-02 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Branches_AddButton_IsVisible()
    {
        var (ctx, page) = await LoginAndGoToBranchesAsync();
        try
        {
            var branchPage = new BranchesPage(page);
            await branchPage.AddBranchButton.WaitForAsync();
            var isVisible = await branchPage.AddBranchButton.IsVisibleAsync();
            isVisible.Should().BeTrue();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-BR-03 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Branches_ClickAddBranch_OpensModal()
    {
        var (ctx, page) = await LoginAndGoToBranchesAsync();
        try
        {
            var branchPage = new BranchesPage(page);
            await branchPage.AddBranchButton.ClickAsync();

            // Modal should appear
            await branchPage.Modal.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            var isVisible = await branchPage.Modal.IsVisibleAsync();
            isVisible.Should().BeTrue();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-BR-04 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Branches_CancelModal_ClosesModal()
    {
        var (ctx, page) = await LoginAndGoToBranchesAsync();
        try
        {
            var branchPage = new BranchesPage(page);
            await branchPage.AddBranchButton.ClickAsync();
            await branchPage.Modal.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            await branchPage.CancelButton.ClickAsync();
            await branchPage.Modal.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3_000 });

            var isVisible = await branchPage.Modal.IsVisibleAsync();
            isVisible.Should().BeFalse();
        }
        finally { await ctx.DisposeAsync(); }
    }
}
