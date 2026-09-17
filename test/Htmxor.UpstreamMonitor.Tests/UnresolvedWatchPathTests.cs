using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class UnresolvedWatchPathTests
{
	// The exact wrong path #219 shipped for two releases before it was noticed: see issue #232.
	private const string WrongFilePath = "src/Components/Endpoints/src/CacheView/CacheViewTextWriter.cs";
	private const string UnmatchedPrefix = "src/Components/Endpoints/src/CacheView/Retired";

	[Fact]
	public async Task File_watch_at_a_path_that_never_existed_upstream_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(WrongFilePath);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		Assert.NotEqual(MonitorStatus.Current, result.Status);
		AssertNamesTheUnresolvedPath(result, WrongFilePath);
	}

	[Fact]
	public async Task Prefix_watch_matching_no_upstream_files_is_not_reported_as_current()
	{
		var watch = Fixture.Watch(UnmatchedPrefix, WatchMatch.Prefix);
		var transport = UnrelatedChangeTransport();

		var result = await Fixture.Application(transport).RunAsync(ProviderInventoryTests.Request(watch));

		Assert.NotEqual(MonitorStatus.Current, result.Status);
		AssertNamesTheUnresolvedPath(result, UnmatchedPrefix);
	}

	// The exact path must be named so a reviewer can act on it, and both artifacts named in the
	// contract's observation seam (JSON and Markdown reports) must carry it, not just one.
	private static void AssertNamesTheUnresolvedPath(MonitorResult result, string path)
	{
		Assert.Contains(path, result.JsonReport, StringComparison.Ordinal);
		Assert.Contains(path, result.MarkdownReport, StringComparison.Ordinal);
	}

	// The wrong or unmatched watch path can never appear in a real GitHub compare response, so an
	// unrelated changed file is the faithful shape of what the provider actually returns.
	private static FakeGitHubTransport UnrelatedChangeTransport()
	{
		var transport = ProviderInventoryTests.TargetTransport();
		transport.AddJson(
			$"/repos/dotnet/aspnetcore/compare/{Fixture.BaselineCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated/File.cs", status = "modified" } } }));
		return transport;
	}
}
