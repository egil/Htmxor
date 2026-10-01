using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Htmxor.AspNetCore10;

// #263's own red-contract cases for streamed JavaScript initializers, compiled for both net10.0 and net11.0 (see
// the csproj). Reuses Issue260App's existing inherited-streaming page and Issue260Gate's "inherited-child"
// waypoint, and Issue260Host's configureBuilder hook to supply a configured modules.json the same way
// Issue191's and Issue214's own hosts already do, rather than adding a fourth copy of that setup.
public sealed class Issue263StreamingInitializersTests
{
	private const string Path = "/issue-260/inherited-streaming";
	private const string ModulesContent = "[\"issue-263\"]";

	[Fact]
	public async Task A_streamed_ordinary_response_emits_configured_initializers_like_stock()
	{
		await using var fixture = Issue263Initializers.Create(ModulesContent);
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, configureBuilder: fixture.Configure);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, configureBuilder: fixture.Configure);

		var stockResult = await RunAsync(stock, enhancedNavigation: false);
		var candidateResult = await RunAsync(candidate, enhancedNavigation: false);

		// Stock's own oracle: a streamed response writes the base64-encoded modules.json contents once, at the
		// start of its streaming updates, immediately after the (here empty) framing marker.
		var expectedMarker = $"<!--Blazor-Web-Initializers:{Convert.ToBase64String(Encoding.UTF8.GetBytes(ModulesContent))}-->";
		Assert.Contains(expectedMarker, stockResult.Body, StringComparison.Ordinal);
		AssertStreamed(stockResult.Body);
		Assert.Equal(
			Issue263FramingMarkup.Normalize(stockResult.Body, stockResult.FramingHeaderValue),
			Issue263FramingMarkup.Normalize(candidateResult.Body, candidateResult.FramingHeaderValue));
	}

	[Fact]
	public async Task A_streamed_enhanced_navigation_response_omits_initializers_like_stock()
	{
		await using var fixture = Issue263Initializers.Create(ModulesContent);
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, configureBuilder: fixture.Configure);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, configureBuilder: fixture.Configure);

		var stockResult = await RunAsync(stock, enhancedNavigation: true);
		var candidateResult = await RunAsync(candidate, enhancedNavigation: true);

		Assert.DoesNotContain("<!--Blazor-Web-Initializers:", stockResult.Body, StringComparison.Ordinal);
		AssertStreamed(stockResult.Body);
		Assert.Equal(
			Issue263FramingMarkup.Normalize(stockResult.Body, stockResult.FramingHeaderValue),
			Issue263FramingMarkup.Normalize(candidateResult.Body, candidateResult.FramingHeaderValue));
	}

	// Proves each case actually exercised SendStreamingUpdatesAsync, the call this issue's own cases cover for
	// the first time, rather than the non-streamed EmitInitializersIfNecessary branch #214 already pins: without
	// this, a race that let the response complete before the body read would still pass with no initializer
	// assertion ever having observed a streamed response.
	private static void AssertStreamed(string body)
	{
		Assert.Contains("<blazor-ssr>", body, StringComparison.Ordinal);
		Assert.Contains("<template blazor-component-id=", body, StringComparison.Ordinal);
	}

	// Shares Issue263FramingRun's release-after-headers ordering with the framing cases: releasing before the
	// response starts would let the child finish inside the first render pass and silently fall back to the
	// non-streamed EmitInitializersIfNecessary branch #214 already covers.
	private static Task<Issue263FramingResult> RunAsync(Issue260Host host, bool enhancedNavigation)
		=> Issue263FramingRun.RunReleasingAfterHeadersAsync(host, Path, enhancedNavigation);
}

// Configures a fresh, isolated WebRootFileProvider carrying one modules.json, the same mechanism Issue191's own
// JavaScriptInitializers fixture and Issue214Host use; kept local to this file because Issue191's fixture
// compiles only for net10.0 (see the csproj) while this issue's cases need the same setup on both targets.
internal sealed class Issue263Initializers(string directory, string contents) : IAsyncDisposable
{
	public static Issue263Initializers Create(string contents)
	{
		var directory = Path.Combine(Path.GetTempPath(), $"htmxor-issue-263-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		return new(directory, contents);
	}

	public void Configure(WebApplicationBuilder builder)
	{
		File.WriteAllText(Path.Combine(directory, $"{builder.Environment.ApplicationName}.modules.json"), contents);
		builder.Environment.WebRootFileProvider = new PhysicalFileProvider(directory);
	}

	public ValueTask DisposeAsync()
	{
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, recursive: true);
		}

		return ValueTask.CompletedTask;
	}
}
