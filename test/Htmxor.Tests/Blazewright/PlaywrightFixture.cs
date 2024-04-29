using System.Text;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;

namespace Htmxor.Blazewright;

public class PlaywrightFixture : IAsyncLifetime
{
    private readonly List<IBrowserContext> contexts = [];

    private readonly BlazorApplicationFactory<global::Program> host;

    public string ServerAddress { get; private set; } = null!;

    public IServiceProvider Services { get; private set; } = null!;

    public IPlaywright Playwright { get; private set; } = null!;

    public IBrowser Browser { get; internal set; } = null!;

    public string BrowserName { get; internal set; } = null!;

    public IBrowserType BrowserType { get; private set; } = null!;

    public PlaywrightFixture()
    {
        host = new BlazorApplicationFactory<global::Program>();
    }

    public async Task InitializeAsync()
    {
        ServerAddress = host.ServerAddress;

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Playwright.Selectors.SetTestIdAttribute("data-testid");
        BrowserName = PlaywrightSettingsProvider.BrowserName;
        BrowserType = Playwright[BrowserName];
        Browser = await CreateBrowser(BrowserType);
        Services = host.Services;
    }

    public async Task<IBrowserContext> NewContext()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        contexts.Add(context);
        return context;
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            contexts
                .Select(x => x.DisposeAsync().AsTask())
                .Append(Browser.DisposeAsync().AsTask())
                .Append(host.DisposeAsync().AsTask()));
        Playwright.Dispose();
        contexts.Clear();
    }

    public virtual BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            Locale = "en-US",
            ColorScheme = ColorScheme.Light,
            BaseURL = ServerAddress,
            IgnoreHTTPSErrors = true,
        };
    }

    private static async Task<IBrowser> CreateBrowser(IBrowserType browserType)
    {
        return await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }
}
