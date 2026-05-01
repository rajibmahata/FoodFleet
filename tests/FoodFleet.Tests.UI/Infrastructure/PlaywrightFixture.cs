using Microsoft.Playwright;

namespace FoodFleet.Tests.UI.Infrastructure;

/// <summary>
/// xUnit collection fixture: one browser instance shared across all tests in the [Collection].
/// Each test gets its own isolated BrowserContext (fresh cookies / storage).
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public const string AdminBaseUrl = "http://localhost:5149";
    public const string ApiBaseUrl   = "http://localhost:5242";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,   // set true for CI
            SlowMo   = 100
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    /// <summary>Creates a new isolated browser context + page for one test.</summary>
    public async Task<(IBrowserContext ctx, IPage page)> NewPageAsync()
    {
        var ctx  = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL         = AdminBaseUrl,
            ViewportSize    = new ViewportSize { Width = 1440, Height = 900 },
            RecordVideoDir  = "videos/"
        });
        ctx.SetDefaultTimeout(30_000);
        var page = await ctx.NewPageAsync();
        return (ctx, page);
    }
}

[CollectionDefinition(nameof(PlaywrightCollection))]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
