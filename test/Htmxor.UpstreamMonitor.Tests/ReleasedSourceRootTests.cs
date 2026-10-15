using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

public sealed class ReleasedSourceRootTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Released_subtree_reports_a_patch_missing_from_the_original_repo_comparison(bool largeComparison)
    {
        using var fixture = new ReleasedSourceFixture(largeComparison);
        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.Drift, result.Status);
        Assert.Equal(new UpstreamRevision("v10.0.13", ReleasedSourceFixture.Current), result.Upstream);
        Assert.Equal(new SourceChange("src/Widget.cs", ChangeKind.Changed, ReviewClassification.ParityRequired), Assert.Single(result.SourceChanges));
        Assert.Contains($"https://github.com/dotnet/dotnet/blob/{ReleasedSourceFixture.Current}/src/aspnetcore/src/Widget.cs",
            Assert.Single(result.Issues).Body, StringComparison.Ordinal);
        Assert.Contains(ReleasedSourceFixture.Baseline, result.JsonReport, StringComparison.Ordinal);
        Assert.DoesNotContain("src/aspnetcore/src/Widget.cs", result.SourceChanges.Select(change => change.Path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tree_identity_distinguishes_commit_resolution_from_known_tree_requests(bool knownTree)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.ReplaceTreeIdentity(knownTree);

        var result = await fixture.RunAsync();

        Assert.Equal(knownTree ? MonitorStatus.InfrastructureError : MonitorStatus.Drift, result.Status);
    }

    [Fact]
    public async Task Runtime_release_catalog_selects_the_same_tag_in_the_build_repository()
    {
        using var fixture = new ReleasedSourceFixture();
        var result = await fixture.RunLatestRuntimeAsync();

        Assert.Equal(MonitorStatus.Drift, result.Status);
        Assert.Equal(new UpstreamRevision("v10.0.13", ReleasedSourceFixture.Current), result.Upstream);
        Assert.Single(result.SourceChanges);
    }

    [Fact]
    public async Task Rooted_partial_type_preserves_API_and_descendant_source_coverage()
    {
        using var fixture = new ReleasedSourceFixture(prefix: true);
        fixture.AddPartialAndDescendantChanges();
        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.Drift, result.Status);
        Assert.Equal(new[] { "src/Widget.Extra.cs", "src/Widget/Child.cs" }, result.SourceChanges.Select(change => change.Path));
        Assert.Contains(result.ApiChanges, change => change.Kind == ChangeKind.Added && change.Signature.Contains("NewHook", StringComparison.Ordinal));
        Assert.DoesNotContain(result.ApiChanges, change => change.Signature.Contains("DescendantOnly", StringComparison.Ordinal));
        Assert.DoesNotContain(result.ApiChanges, change => change.Signature.Contains("UnrootedDecoyHook", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Released_inventory_preserves_added_and_removed_file_findings(bool removed)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.RemoveFromOneRevision(removed);
        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.Drift, result.Status);
        Assert.Equal(new SourceChange("src/Widget.cs", removed ? ChangeKind.Removed : ChangeKind.Added,
            ReviewClassification.ParityRequired), Assert.Single(result.SourceChanges));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removed_directory_preserves_wrong_kind_finding_and_descendant_file_drift(bool directoryFirst)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.RemoveWatchedDirectory(directoryFirst);

        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
        Assert.Equal(new UnresolvedWatch("src/Removed", WatchFinding.ExistsAsDirectory), Assert.Single(result.UnresolvedWatches!));
        Assert.Equal(new SourceChange("src/Removed/Child.cs", ChangeKind.Removed, ReviewClassification.ParityRequired),
            Assert.Single(result.SourceChanges));
        Assert.Equal(2, result.Issues.Count);
        var drift = Assert.Single(result.Issues, issue => issue.Identity == "aspnetcore-10-upstream-drift");
        var unresolved = Assert.Single(result.Issues, issue => issue.Identity == "aspnetcore-10-unresolved-watch");
        Assert.Contains("src/Removed/Child.cs", drift.Body, StringComparison.Ordinal);
        Assert.Contains("src/Removed", unresolved.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("src/Removed/Child.cs", unresolved.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task File_directory_transition_reports_the_file_addition_or_removal(bool fileRemoved)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.ReplaceFileWithDirectory(fileRemoved);

        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.Drift, result.Status);
        Assert.Equal(new SourceChange("src/Widget.cs", fileRemoved ? ChangeKind.Removed : ChangeKind.Added,
            ReviewClassification.ParityRequired), Assert.Single(result.SourceChanges));
    }

    [Theory]
    [InlineData("dir", nameof(WatchFinding.ExistsAsDirectory))]
    [InlineData("symlink", nameof(WatchFinding.ExistsAsSymlink))]
    [InlineData("submodule", nameof(WatchFinding.ExistsAsSubmodule))]
    public async Task Root_mapping_does_not_turn_wrong_kind_watches_into_source_files(string kind, string expected)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.SetUnchangedWatchKind(kind);
        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.UnresolvedWatch, result.Status);
        Assert.Equal(new UnresolvedWatch("src/Widget.cs", Enum.Parse<WatchFinding>(expected)), Assert.Single(result.UnresolvedWatches!));
        Assert.Contains($"/tree/{ReleasedSourceFixture.Baseline}/src/aspnetcore/src/Widget.cs",
            Assert.Single(result.Issues).Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("truncated")]
    [InlineData("missing-truncated")]
    [InlineData("missing-tree")]
    [InlineData("duplicate")]
    [InlineData("missing-sha")]
    [InlineData("unsafe-path")]
    [InlineData("missing-parent")]
    [InlineData("wrong-root-kind")]
    public async Task Incomplete_or_inconsistent_released_inventory_cannot_claim_current(string fault)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.CorruptInventory(fault);
        var result = await fixture.RunAsync();

        Assert.Equal(MonitorStatus.InfrastructureError, result.Status);
        Assert.NotEmpty(result.InfrastructureError!);
        Assert.Empty(result.Issues);
    }

    [Theory]
    [InlineData("../aspnetcore")]
    [InlineData("/src/aspnetcore")]
    [InlineData("src//aspnetcore")]
    [InlineData("src/aspnetcore?ref=other")]
    [InlineData("src\\aspnetcore")]
    public void Unsafe_source_root_is_rejected_before_provider_access(string root)
    {
        using var fixture = new ReleasedSourceFixture();
        fixture.WriteManifest(root);
        Assert.ThrowsAny<Exception>(() => WatchManifestFile.Read(fixture.Repository.Path));
    }
}

internal sealed class ReleasedSourceFixture : IDisposable
{
    internal const string Baseline = "1111111111111111111111111111111111111111";
    internal const string Current = "2222222222222222222222222222222222222222";
    private const string OldSubtree = "3333333333333333333333333333333333333333";
    private const string NewSubtree = "4444444444444444444444444444444444444444";
    private const string OldSrc = "5555555555555555555555555555555555555555";
    private const string NewSrc = "6666666666666666666666666666666666666666";
    private const string OldBlob = "7777777777777777777777777777777777777777";
    private const string NewBlob = "8888888888888888888888888888888888888888";
    private readonly FakeGitHubTransport transport = new();
    private readonly bool prefix;
    public TemporaryRepository Repository { get; } = new();

    public ReleasedSourceFixture(bool largeComparison = false, bool prefix = false)
    {
        this.prefix = prefix;
        WriteManifest("src/aspnetcore");
        transport.AddRepeatingJson("/repos/dotnet/dotnet/git/ref/tags/v10.0.13",
            JsonSerializer.Serialize(new { @object = new { type = "commit", sha = Current } }));
        // OriginalRepoCommitHash can be unchanged while a VMR patch changes the shipped source.
        var unrelated = Enumerable.Range(0, largeComparison ? 300 : 0)
            .Select(i => new { filename = $"src/runtime/Unrelated{i}.cs", status = "modified" });
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/compare/{Baseline}...{Current}", JsonSerializer.Serialize(new { files = unrelated }));
        AddTraversal(Baseline, OldSrc, OldSubtree);
        AddTraversal(Current, NewSrc, NewSubtree);
        SetSubtree(OldSubtree, OldBlob);
        SetSubtree(NewSubtree, NewBlob);
        AddSource("src/Widget.cs", Baseline, "public partial class Widget { protected void Existing() {} }");
        AddSource("src/Widget.cs", Current, "// Shipped VMR patch\npublic partial class Widget { protected void Existing() {} }");
    }

    public void WriteManifest(string sourceRoot) => Repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json",
        JsonSerializer.Serialize(new
        {
            repository = "dotnet/dotnet", releaseRepository = "dotnet/aspnetcore", sourceRoot,
            reviewed = new { tag = "v10.0.11", commit = Baseline },
            watches = new[] { new { path = prefix ? "src/Widget" : "src/Widget.cs", match = prefix ? "prefix" : "file",
                api = prefix ? "subclass" : "none", relationship = "reimplements", dependencies = Array.Empty<string>() } },
        }));

    public Task<MonitorResult> RunAsync() => Fixture.Application(transport).RunAsync(
        new MonitorRequest(WatchManifestFile.Read(Repository.Path), 10, RequestedTag: "v10.0.13"));

    public Task<MonitorResult> RunLatestRuntimeAsync()
    {
        transport.AddRepeatingJson("/repos/dotnet/aspnetcore/releases?per_page=100",
            "[{\"tag_name\":\"v10.0.13\",\"draft\":false,\"prerelease\":false}]");
        transport.AddRepeatingJson("/repos/dotnet/dotnet/releases?per_page=100",
            "[{\"tag_name\":\"v10.0.402\",\"draft\":false,\"prerelease\":false}]");
        const string sdkCommit = "dddddddddddddddddddddddddddddddddddddddd";
        AddTraversal(sdkCommit, OldSrc, OldSubtree);
        transport.AddRepeatingJson("/repos/dotnet/dotnet/git/ref/tags/v10.0.402",
            JsonSerializer.Serialize(new { @object = new { type = "commit", sha = sdkCommit } }));
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/compare/{Baseline}...{sdkCommit}", "{\"files\":[]}");
        return Fixture.Application(transport).RunAsync(new MonitorRequest(WatchManifestFile.Read(Repository.Path), 10));
    }

    private void AddTraversal(string commit, string src, string subtree)
    {
        Tree(commit, [Entry("src", "tree", src, "040000")]);
        Tree(src, [Entry("aspnetcore", "tree", subtree, "040000")]);
    }

    public void ReplaceTreeIdentity(bool knownTree)
    {
        var request = knownTree ? NewSrc : Current;
        object[] entries = knownTree
            ? [Entry("aspnetcore", "tree", NewSubtree, "040000")]
            : [Entry("src", "tree", NewSrc, "040000")];
        transport.ReplaceJson(TreeUrl(request), JsonSerializer.Serialize(new
        {
            sha = new string('e', 40), truncated = false, tree = entries,
        }));
    }

    private void SetSubtree(string subtree, string blob) => Tree(subtree,
        [Entry("src", "tree", subtree == OldSubtree ? new string('a', 40) : new string('b', 40), "040000"), Entry("src/Widget.cs", "blob", blob)], recursive: true);

    private void Tree(string sha, object[] entries, bool recursive = false) => transport.AddRepeatingJson(
        TreeUrl(sha, recursive), JsonSerializer.Serialize(new { sha, truncated = false, tree = entries }));

    private static string TreeUrl(string sha, bool recursive = false) =>
        $"/repos/dotnet/dotnet/git/trees/{sha}" + (recursive ? "?recursive=1" : "");

    private static object Entry(string path, string type, string sha, string mode = "100644") => new { path, type, sha, mode };

    private void AddSource(string path, string commit, string source)
    {
        var content = Fixture.GitHubContentText(source);
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/contents/src/aspnetcore/{path}?ref={commit}", content);
        // Keep unrooted downloads valid but distinguish their API delta from the shipped source.
        var decoy = Fixture.GitHubContentText(source.Replace("NewHook", "UnrootedDecoyHook", StringComparison.Ordinal));
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/contents/{path}?ref={commit}", decoy);
    }

    public void RemoveFromOneRevision(bool removed) => Tree(removed ? NewSubtree : OldSubtree,
        [Entry("src", "tree", new string('a', 40), "040000")], recursive: true);

    public void RemoveWatchedDirectory(bool directoryFirst)
    {
        var paths = directoryFirst ? new[] { "src/Removed", "src/Removed/Child.cs" } : new[] { "src/Removed/Child.cs", "src/Removed" };
        Repository.Write("eng/Htmxor.UpstreamMonitor/upstream-watch.json", JsonSerializer.Serialize(new
        {
            repository = "dotnet/dotnet", sourceRoot = "src/aspnetcore",
            reviewed = new { tag = "v10.0.11", commit = Baseline },
            watches = paths.Select(path => new { path, match = "file", api = "none", relationship = "reimplements", dependencies = Array.Empty<string>() }),
        }));
        Tree(OldSubtree, [Entry("src", "tree", OldSrc, "040000"), Entry("src/Removed", "tree", OldBlob, "040000"),
            Entry("src/Removed/Child.cs", "blob", NewBlob)], recursive: true);
        Tree(NewSubtree, [Entry("src", "tree", NewSrc, "040000")], recursive: true);
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/contents/src/aspnetcore/src/Removed?ref={Baseline}",
            JsonSerializer.Serialize(new[] { new { path = "src/aspnetcore/src/Removed/Child.cs", type = "file" } }));
        AddSource("src/Removed/Child.cs", Baseline, "public class Child { }");
    }

    public void ReplaceFileWithDirectory(bool fileRemoved)
    {
        Tree(fileRemoved ? NewSubtree : OldSubtree,
            [Entry("src", "tree", OldSrc, "040000"), Entry("src/Widget.cs", "tree", NewSrc, "040000"),
             Entry("src/Widget.cs/Child.cs", "blob", NewBlob)], recursive: true);
        transport.ReplaceJson($"/repos/dotnet/dotnet/contents/src/aspnetcore/src/Widget.cs?ref={(fileRemoved ? Current : Baseline)}",
            JsonSerializer.Serialize(new[] { new { path = "src/aspnetcore/src/Widget.cs/Child.cs", type = "file" } }));
    }

    public void SetUnchangedWatchKind(string kind)
    {
        var type = kind switch { "dir" => "tree", "symlink" => "blob", _ => "commit" };
        var mode = kind switch { "dir" => "040000", "symlink" => "120000", _ => "160000" };
        object[] entries = [Entry("src", "tree", new string('a', 40), "040000"), Entry("src/Widget.cs", type, OldBlob, mode)];
        Tree(OldSubtree, entries, recursive: true);
        Tree(NewSubtree, entries, recursive: true);
        var content = kind == "dir" ? "[]" : JsonSerializer.Serialize(new { type = kind });
        transport.ReplaceJson($"/repos/dotnet/dotnet/contents/src/aspnetcore/src/Widget.cs?ref={Baseline}", content);
    }

    public void AddPartialAndDescendantChanges()
    {
        PartialTree(OldSubtree, OldBlob);
        PartialTree(NewSubtree, NewBlob);
        PartialSources(Baseline, "public partial class Widget { }");
        PartialSources(Current, "public partial class Widget { protected void NewHook() {} }");
    }

    private void PartialTree(string subtree, string changedBlob) => Tree(subtree,
        [Entry("src", "tree", subtree == OldSubtree ? new string('a', 40) : new string('b', 40), "040000"), Entry("src/Widget.cs", "blob", OldBlob),
         Entry("src/Widget.Extra.cs", "blob", changedBlob), Entry("src/Widget", "tree", NewSrc, "040000"),
         Entry("src/Widget/Child.cs", "blob", changedBlob)], recursive: true);

    private void PartialSources(string commit, string extra)
    {
        AddSource("src/Widget.Extra.cs", commit, extra);
        AddSource("src/Widget/Child.cs", commit, "public class Widget { protected void DescendantOnly() {} }");
        var listing = new[] { new { path = "src/aspnetcore/src/Widget.cs", type = "file" },
            new { path = "src/aspnetcore/src/Widget.Extra.cs", type = "file" }, new { path = "src/aspnetcore/src/Widget", type = "dir" } };
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/contents/src/aspnetcore/src?ref={commit}", JsonSerializer.Serialize(listing));
        transport.AddRepeatingJson($"/repos/dotnet/dotnet/contents/src?ref={commit}",
            JsonSerializer.Serialize(listing.Select(entry => new { path = entry.path["src/aspnetcore/".Length..], entry.type })));
    }

    public void CorruptInventory(string fault)
    {
        if (fault == "wrong-root-kind")
        {
            // A bypassed kind check still reaches a complete subtree, not an unrelated 404 or malformed tree.
            Tree(NewSrc, [Entry("aspnetcore", "blob", NewSubtree)]);
            return;
        }
        var normal = new[] { Entry("src", "tree", new string('b', 40), "040000"), Entry("src/Widget.cs", "blob", NewBlob) };
        var response = fault switch
        {
            "truncated" => JsonSerializer.Serialize(new { sha = NewSubtree, truncated = true, tree = normal }),
            "missing-truncated" => JsonSerializer.Serialize(new { sha = NewSubtree, tree = normal }),
            "missing-tree" => JsonSerializer.Serialize(new { sha = NewSubtree, truncated = false }),
            _ => InvalidEntries(fault),
        };
        transport.ReplaceJson(TreeUrl(NewSubtree, true), response);
    }

    private static string InvalidEntries(string fault)
    {
        object[] entries = fault switch
        {
            "duplicate" => [Entry("src", "tree", new string('b', 40), "040000"), Entry("src/Widget.cs", "blob", NewBlob), Entry("src/Widget.cs", "blob", OldBlob)],
            "missing-sha" => [Entry("src", "tree", new string('b', 40), "040000"), new { path = "src/Widget.cs", type = "blob", mode = "100644" }],
            "unsafe-path" => [Entry("..", "blob", NewBlob)],
            _ => [Entry("src/Widget.cs", "blob", NewBlob)],
        };
        return JsonSerializer.Serialize(new { sha = NewSubtree, truncated = false, tree = entries });
    }

    public void Dispose() => Repository.Dispose();
}
