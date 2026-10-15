using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class BehaviourDependencyTests
{
	private const string Relationship = "depends-on-behaviour";
	private const string Builder = "src/Htmxor/Builder/HtmxorComponentEndpointRouteBuilderExtensions.cs";
	private const string PageAssembly = "src/Htmxor/Builder/HtmxorRoutePageAssembly.cs";
	private static readonly (string Upstream, string Local)[] dependencies =
	[
		("src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs", Builder),
		("src/Components/Endpoints/src/Discovery/IRazorComponentApplication.cs", PageAssembly),
		("src/Components/Endpoints/src/Discovery/PageCollectionBuilder.cs", Builder),
	];

	[Theory]
	[InlineData(10, false)]
	[InlineData(11, false)]
	[InlineData(10, true)]
	[InlineData(11, true)]
	public void Source_owned_behaviour_dependency_with_missing_or_mismatched_watch_is_reported(int major, bool mismatched)
	{
		using var repository = new TemporaryRepository();
		foreach (var group in dependencies.GroupBy(dependency => dependency.Local))
		{
			repository.Write(group.Key, string.Join('\n', group.Select(dependency =>
				$"// Htmxor upstream dependency: {dependency.Upstream} | {Relationship}")));
		}
		var framework = Framework(major);
		var manifest = Fixture.ManifestFor(framework, mismatched
			? dependencies.Select(dependency => Fixture.Watch(dependency.Upstream,
				relationship: WatchRelationship.Reimplements, dependencies: [dependency.Local])).ToArray()
			: []);

		var omissions = ManifestDependencyPolicy.FindUntrackedDependencies(repository.Path, manifest);

		Assert.Equal(dependencies.Select(dependency => $"{dependency.Local}|{dependency.Upstream}|DependsOnBehaviour").Order(StringComparer.Ordinal),
			omissions.Select(dependency => $"{dependency.LocalPath}|{dependency.UpstreamPath}|{dependency.Relationship}").Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData(10)]
	[InlineData(11)]
	public void Route_registration_sources_declare_framework_specific_behaviour_dependencies(int major)
	{
		var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

		var discovered = LocalFrameworkDependencyDiscovery.Discover(root, Framework(major))
			.Where(dependency => dependency.LocalPath is Builder or PageAssembly);

		var expected = major == 11
			? dependencies.Append((Upstream: "src/DefaultBuilder/src/Internal/DefaultCsrfProtection.cs", Local: Builder))
			: dependencies;

		Assert.Equal(expected.Select(dependency => $"{dependency.Local}|{dependency.Upstream}|DependsOnBehaviour").Order(StringComparer.Ordinal),
			discovered.Select(dependency => $"{dependency.LocalPath}|{dependency.UpstreamPath}|{dependency.Relationship}").Order(StringComparer.Ordinal));
	}

	[Theory]
	[InlineData(10, "added", "added")]
	[InlineData(11, "added", "added")]
	[InlineData(10, "modified", "changed")]
	[InlineData(11, "modified", "changed")]
	[InlineData(10, "removed", "removed")]
	[InlineData(11, "removed", "removed")]
	public async Task Behaviour_source_drift_reports_the_watched_path_for_implementation_review(int major, string providerStatus, string reportKind)
	{
		using var repository = new TemporaryRepository();
		WriteManifest(repository, Relationship);
		var manifest = WatchManifestFile.Read(repository.Path);
		var framework = manifest.Frameworks.Single(framework => framework.MajorVersion == major);
		var tag = TargetTag(major);
		var transport = new FakeGitHubTransport();
		transport.AddJson($"/repos/dotnet/aspnetcore/git/ref/tags/{tag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Fixture.TargetCommit } }));
		transport.AddJson($"/repos/dotnet/aspnetcore/compare/{framework.ReviewedCommit}...{Fixture.TargetCommit}",
			JsonSerializer.Serialize(new { files = dependencies.Select(dependency => new { filename = dependency.Upstream, status = providerStatus }) }));

		var result = await Fixture.Application(transport).RunAsync(new MonitorRequest(manifest, framework, tag, framework.ReviewedCommit));

		Assert.Equal(MonitorStatus.Drift, result.Status);
		ReportAssertions.Equal(result, new ReportExpectation("drift", new(framework.ReviewedTag, framework.ReviewedCommit),
			new(tag, Fixture.TargetCommit), dependencies.Select(dependency =>
				new SourceReportRow(dependency.Upstream, reportKind, "implementation-review")).ToArray(), [], null));
		var issue = Assert.Single(result.Issues);
		Assert.All(dependencies, dependency =>
		{
			Assert.Contains($"Implementation review | {reportKind} | {dependency.Upstream}", issue.Body);
		});
	}

	[Theory]
	[InlineData("depends-on-behaviour")]
	[InlineData("DEPENDS-ON-BEHAVIOUR")]
	[InlineData("Depends-On-Behaviour")]
	public void Behaviour_manifest_relationship_is_case_insensitive(string relationship)
	{
		using var repository = new TemporaryRepository();
		WriteManifest(repository, relationship);

		var manifest = WatchManifestFile.Read(repository.Path);

		Assert.Equal(dependencies.Select(dependency => $"{dependency.Upstream}|File|None|DependsOnBehaviour|{dependency.Local}"),
			manifest.Targets.Select(watch => $"{watch.Path}|{watch.Match}|{watch.ApiSurface}|{watch.Relationship}|{string.Join(',', watch.LocalDependencies)}"));
	}

	[Theory]
	[InlineData(10, false)]
	[InlineData(11, false)]
	[InlineData(10, true)]
	[InlineData(11, true)]
	public async Task Behaviour_watches_without_matching_changes_remain_current(int major, bool unrelatedChange)
	{
		using var repository = new TemporaryRepository();
		WriteManifest(repository, Relationship);
		var manifest = WatchManifestFile.Read(repository.Path);
		var framework = manifest.Frameworks.Single(framework => framework.MajorVersion == major);
		var tag = unrelatedChange ? TargetTag(major) : framework.ReviewedTag;
		var commit = unrelatedChange ? Fixture.TargetCommit : framework.ReviewedCommit;
		var transport = new FakeGitHubTransport();
		transport.AddJson($"/repos/dotnet/aspnetcore/git/ref/tags/{tag}",
			JsonSerializer.Serialize(new { @object = new { type = "commit", sha = commit } }));
		if (unrelatedChange)
		{
			transport.AddJson($"/repos/dotnet/aspnetcore/compare/{framework.ReviewedCommit}...{commit}",
				JsonSerializer.Serialize(new { files = new[] { new { filename = "src/Unrelated.cs", status = "modified" } } }));
		}
		foreach (var dependency in dependencies)
		{
			transport.AddJson($"/repos/dotnet/aspnetcore/contents/{dependency.Upstream}?ref={framework.ReviewedCommit}",
				Fixture.GitHubContentText("public class Dependency { }"));
		}

		var result = await Fixture.Application(transport).RunAsync(new MonitorRequest(manifest, framework, tag, framework.ReviewedCommit));

		Assert.Equal(MonitorStatus.Current, result.Status);
		Assert.Empty(result.Issues);
		Assert.Empty(result.SourceChanges);
		Assert.Empty(result.ApiChanges);
		ReportAssertions.Equal(result, new ReportExpectation("current", new(framework.ReviewedTag, framework.ReviewedCommit),
			new(tag, commit), [], [], null));
	}

	[Theory]
	[InlineData("string")]
	[InlineData("block comment")]
	public void Behaviour_marker_outside_a_line_comment_is_inert(string location)
	{
		using var repository = new TemporaryRepository();
		var marker = $"// Htmxor upstream dependency: {dependencies[0].Upstream} | {Relationship}";
		var source = location == "string"
			? $"class Dependency {{ string marker = {JsonSerializer.Serialize(marker)}; }}"
			: $"/*\n{marker}\n*/\nclass Dependency {{ }}";
		repository.Write(Builder, source);

		var discovered = LocalFrameworkDependencyDiscovery.Discover(repository.Path);

		Assert.Empty(discovered);
	}

	private static string TargetTag(int major) => major == 10 ? "v10.0.12" : "v11.0.0-rc.2.26430.1";

	private static FrameworkBaseline Framework(int major) => major == 10 ? Fixture.Net10Framework() : Fixture.Net11Framework();

	private static void WriteManifest(TemporaryRepository repository, string relationship) =>
		repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", JsonSerializer.Serialize(new
		{
			repository = Fixture.Repository,
			frameworks = new[] { Fixture.Net10Framework(), Fixture.Net11Framework() }.Select(framework => new
			{
				targetFramework = framework.TargetFramework, majorVersion = framework.MajorVersion,
				allowsPrerelease = framework.AllowsPrerelease, referencePackVersion = framework.ReferencePackVersion,
				reviewed = new { tag = framework.ReviewedTag, commit = framework.ReviewedCommit },
			}),
			watches = dependencies.Select(dependency => new
			{
				path = dependency.Upstream, match = "file", api = "none", relationship,
				dependencies = new[] { dependency.Local },
			}),
		}));
}
