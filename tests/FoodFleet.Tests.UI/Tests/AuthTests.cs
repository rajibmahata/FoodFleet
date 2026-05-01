using FoodFleet.Tests.UI.Infrastructure;
using FoodFleet.Tests.UI.Pages;
using FluentAssertions;

namespace FoodFleet.Tests.UI.Tests;

[Collection(nameof(PlaywrightCollection))]
public class AuthTests(PlaywrightFixture fixture)
{
    // ── TC-AUTH-01 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        try
        {
            var login = new LoginPage(page);
            await login.GotoAsync();

            await login.LoginAsync("admin", "admin123");

            // LoginAsync already waits for the URL to leave /login
            page.Url.Should().Contain("/dashboard");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-AUTH-02 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WithInvalidPassword_ShowsErrorMessage()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        try
        {
            var login = new LoginPage(page);
            await login.GotoAsync();

            // Fill and click manually — don't use LoginAsync (which waits for URL change)
            await login.UsernameInput.FillAsync("admin");
            await login.PasswordInput.FillAsync("wrongpassword");
            await login.SignInButton.ClickAsync();

            await login.ErrorMessage.WaitForAsync();
            var errorText = await login.ErrorMessage.InnerTextAsync();
            errorText.Should().NotBeNullOrEmpty();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-AUTH-03 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WhenAlreadyAuthenticated_RedirectsToDashboard()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        try
        {
            // First login
            var login = new LoginPage(page);
            await login.GotoAsync();
            await login.LoginAsync("admin", "admin123");
            // Already on /dashboard after LoginAsync

            // Navigate back to /login — should redirect away
            // GotoAsync waits for the Blazor circuit (button visible or redirect happened).
            // Then OnInitializedAsync detects the stored token and calls Nav.NavigateTo("/dashboard").
            await login.GotoAsync();
            await page.WaitForURLAsync(url => url.Contains("/dashboard"), new() { Timeout = 20_000 });
            page.Url.Should().Contain("/dashboard");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // ── TC-AUTH-04 ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UnauthenticatedAccess_ToDashboard_RedirectsToLogin()
    {
        var (ctx, page) = await fixture.NewPageAsync();
        try
        {
            await page.GotoAsync("/dashboard");
            // Server-side [Authorize] on Dashboard redirects unauthenticated users to
            // /login?ReturnUrl=%2Fdashboard — match any URL containing "login"
            await page.WaitForURLAsync(url => url.Contains("/login"), new() { Timeout = 15_000 });
            page.Url.Should().Contain("/login");
        }
        finally { await ctx.DisposeAsync(); }
    }
}
