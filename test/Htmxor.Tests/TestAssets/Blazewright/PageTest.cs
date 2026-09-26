using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Htmxor.TestAssets.Blazewright;

[Collection("PlaywrightTests")]
public class PageTest : IAsyncLifetime
{
	private readonly PlaywrightFixture fixture;

	public IBrowserContext Context { get; private set; } = null!;

	public IServiceProvider Services { get; }

	public PageTest(PlaywrightFixture fixture)
	{
		this.fixture = fixture;
		Services = fixture.Services;
	}

	public async Task InitializeAsync()
	{
		Context = await fixture.NewContext();
	}

	public Task DisposeAsync()
	{
		return Context.CloseAsync();
	}

	public void SetDefaultExpectTimeout(float timeout) => Assertions.SetDefaultExpectTimeout(timeout);

	// Uniform wiring for every E2E fact through the shared retry mechanism
	// (https://github.com/egil/Htmxor/issues/252): a fresh page per attempt from this test's own
	// Context, with a retry note going to the caller's own ITestOutputHelper.
	public Task RunWithRetryAsync(ITestOutputHelper outputHelper, Func<IPage, Task> body) =>
		BrowserAttemptRetryRunner.RunOnPageAsync(Context, body, outputHelper.WriteLine);

	public ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

	public IPageAssertions Expect(IPage page) => Assertions.Expect(page);

	public IAPIResponseAssertions Expect(IAPIResponse response) => Assertions.Expect(response);
}
