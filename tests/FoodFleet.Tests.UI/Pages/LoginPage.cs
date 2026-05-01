using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Pages;

/// <summary>Page Object Model for /login</summary>
public class LoginPage(IPage page)
{
    private IPage Page { get; } = page;

    public ILocator UsernameInput  => Page.Locator("input[placeholder='admin']");
    public ILocator PasswordInput  => Page.Locator("input[type='password']");
    public ILocator SignInButton   => Page.Locator("button[type='submit']");
    public ILocator ErrorMessage   => Page.Locator(".text-red-700 span");

    public async Task GotoAsync()
    {
        await Page.GotoAsync("/login");
        // Wait for the Blazor InteractiveServer circuit to connect and render the form
        // (prerender: false — the form only appears after the circuit starts).
        // If the user is already authenticated, OnInitializedAsync redirects away before
        // the button appears, so swallow any exception here.
        try
        {
            await SignInButton.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        }
        catch
        {
            // Already redirected (authenticated scenario) or circuit slow — proceed.
        }
    }

    public async Task LoginAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await SignInButton.ClickAsync();
        // Blazor Server navigates via SignalR + history.pushState.
        // Wait for the URL to change away from /login (up to 30 s).
        await Page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 30_000 });
    }
}
