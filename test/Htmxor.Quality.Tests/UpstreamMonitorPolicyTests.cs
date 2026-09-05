using System.Text.Json;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class UpstreamMonitorPolicyTests
{
	private const string MonitorTestProject = "test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj";

	[Fact]
	public void Upstream_is_a_canonical_profile_while_fake_fixture_tests_remain_in_fast_and_full()
	{
		var upstream = QualityOptions.Parse(["check", "--profile", "upstream"]);
		var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "htmxor-upstream-policy"));
		var results = Path.Combine(root, "results");

		Assert.Equal("Upstream", upstream.Profile.ToString());
		foreach (var profile in new[] { QualityProfile.Fast, QualityProfile.Full })
		{
			var plan = QualityPlanFactory.Create(root, results, new QualityOptions(QualityAction.Check, profile));
			Assert.Contains(plan.Tests, test => test.Project == MonitorTestProject);
			Assert.DoesNotContain(
				plan.Tests,
				test => test.Project == "eng/Htmxor.UpstreamMonitor/Htmxor.UpstreamMonitor.csproj");
		}
	}

	[Fact]
	public void Committed_manifest_tracks_the_reviewed_revision_and_required_source_relationships()
	{
		var root = RepositoryLocator.Find();
		var path = Path.Combine(root, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json");

		Assert.True(File.Exists(path), "The upstream monitor manifest must be committed.");
		using var document = JsonDocument.Parse(File.ReadAllText(path));
		var manifest = document.RootElement;
		Assert.Equal("dotnet/aspnetcore", manifest.GetProperty("repository").GetString());
		Assert.Equal("v10.0.11", manifest.GetProperty("reviewed").GetProperty("tag").GetString());
		Assert.Equal(
			"a5383385245bdacc20ec19f30e46090a8154d8da",
			manifest.GetProperty("reviewed").GetProperty("commit").GetString());

		var watches = manifest.GetProperty("watches").EnumerateArray().ToArray();
		AssertWatch(watches, "src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs", "file", "none", root);
		AssertWatch(watches, "src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs", "file", "interface", root);
		AssertWatch(watches, "src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer", "prefix", "subclass", root);
		AssertWatch(watches, "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs", "file", "subclass", root);
		AssertWatch(watches, "src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs", "file", "subclass", root);
		AssertWatch(watches, "src/Components/Components/src/RenderTree/Renderer.cs", "file", "subclass", root);
		AssertWatch(watches, "src/Components/Components/src/Rendering/ComponentState.cs", "file", "subclass", root);
	}

	[Fact]
	public void Upstream_workflow_has_isolated_cadence_permissions_reporting_and_issue_upsert_inputs()
	{
		var root = RepositoryLocator.Find();
		var path = Path.Combine(root, ".github", "workflows", "upstream-monitor.yml");

		Assert.True(File.Exists(path), "The upstream monitor workflow must be separate from ordinary CI.");
		var workflow = File.ReadAllText(path);
		Assert.Matches("cron: '(?:[1-9]|[1-5][0-9]) \\* \\* \\* \\*'", workflow);
		Assert.Contains("workflow_dispatch:", workflow, StringComparison.Ordinal);
		Assert.Contains("repository_dispatch:", workflow, StringComparison.Ordinal);
		Assert.Contains("contents: read", workflow, StringComparison.Ordinal);
		Assert.Contains("issues: write", workflow, StringComparison.Ordinal);
		Assert.Contains("check --profile upstream", workflow, StringComparison.Ordinal);
		Assert.Contains("GH_TOKEN:", workflow, StringComparison.Ordinal);
		Assert.Contains("if: always()", workflow, StringComparison.Ordinal);
		Assert.Matches("retention-days: (?:[1-9]|[1-9][0-9])", workflow);
		Assert.DoesNotContain("check --profile fast", workflow, StringComparison.Ordinal);
		Assert.DoesNotContain("check --profile full", workflow, StringComparison.Ordinal);
	}

	private static void AssertWatch(
		IReadOnlyList<JsonElement> watches,
		string expectedPath,
		string expectedMatch,
		string expectedApi,
		string repositoryRoot)
	{
		var watch = Assert.Single(watches, candidate =>
			candidate.GetProperty("path").GetString() == expectedPath);
		Assert.Equal(expectedMatch, watch.GetProperty("match").GetString());
		Assert.Equal(expectedApi, watch.GetProperty("api").GetString());
		var dependencies = watch.GetProperty("dependencies").EnumerateArray().ToArray();
		Assert.NotEmpty(dependencies);
		Assert.All(dependencies, dependency =>
		{
			var relativePath = Assert.IsType<string>(dependency.GetString());
			Assert.True(
				File.Exists(Path.Combine(repositoryRoot, relativePath)),
				$"Manifest dependency '{relativePath}' must exist.");
		});
	}
}
