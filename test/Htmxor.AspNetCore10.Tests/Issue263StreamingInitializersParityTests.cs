using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

		var stockBody = await RunAsync(stock, enhancedNavigation: false);
		var candidateBody = await RunAsync(candidate, enhancedNavigation: false);

		// Stock's own oracle: EmitInitializersIfNecessary writes the base64-encoded modules.json contents once,
		// immediately after the (possibly empty) streaming framing marker that opens SendStreamingUpdatesAsync's
		// own write -- the same call this issue's streamed cases newly exercise, where only a completed,
		// non-streamed response had paired coverage before.
		var expectedMarker = $"<!--Blazor-Web-Initializers:{Convert.ToBase64String(Encoding.UTF8.GetBytes(ModulesContent))}-->";
		Assert.Contains(expectedMarker, stockBody, StringComparison.Ordinal);
		Assert.Equal(stockBody, candidateBody);
	}

	[Fact]
	public async Task A_streamed_enhanced_navigation_response_omits_initializers_like_stock()
	{
		await using var fixture = Issue263Initializers.Create(ModulesContent);
		await using var stock = await Issue260Host.CreateAsync<Issue260App>(htmxor: false, configureBuilder: fixture.Configure);
		await using var candidate = await Issue260Host.CreateAsync<Issue260App>(htmxor: true, configureBuilder: fixture.Configure);

		var stockBody = await RunAsync(stock, enhancedNavigation: true);
		var candidateBody = await RunAsync(candidate, enhancedNavigation: true);

		Assert.DoesNotContain("<!--Blazor-Web-Initializers:", stockBody, StringComparison.Ordinal);
		Assert.Equal(Issue263FramingMarkup.Normalize(stockBody), Issue263FramingMarkup.Normalize(candidateBody));
	}

	private static async Task<string> RunAsync(Issue260Host host, bool enhancedNavigation)
	{
		var gate = host.Services.GetRequiredService<Issue260Gate>();
		using var request = new HttpRequestMessage(HttpMethod.Get, Path);
		if (enhancedNavigation)
		{
			request.Headers.TryAddWithoutValidation("Accept", Issue264SwitchOnConstants.EnhancedNavigationAccept);
		}

		var responseTask = host.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		await gate.WaitForReachedAsync("inherited-child");
		gate.Release("inherited-child");

		using var response = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
		return await response.Content.ReadAsStringAsync();
	}
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
