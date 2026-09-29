using System.Text;

namespace Htmxor.UpstreamMonitor;

internal sealed class UpstreamMonitorApplication(HttpClient httpClient)
{
	public async Task<MonitorResult> RunAsync(MonitorRequest request, CancellationToken cancellationToken = default)
	{
		UpstreamRevision? upstream = null;
		try
		{
			var repository = new UpstreamRepository(new GitHubApi(httpClient), request.Manifest.Repository);
			upstream = await repository.ResolveAsync(request, cancellationToken);
			var baseline = request.BaselineCommit ?? request.Framework.ReviewedCommit;
			if (upstream.Commit == baseline)
			{
				return await ReportAsync(request, upstream, repository, baseline, [], [], [], [], cancellationToken);
			}
			var files = await repository.CompareAsync(baseline, upstream.Commit, cancellationToken);
			return await CompareWatchedAsync(request, upstream, repository, files, cancellationToken);
		}
		catch (Exception exception)
		{
			return MonitorReports.Create(request, MonitorStatus.InfrastructureError, upstream, [], [], MonitorErrors.SafeMessage(exception));
		}
	}

	private static async Task<MonitorResult> CompareWatchedAsync(MonitorRequest request, UpstreamRevision upstream,
		UpstreamRepository repository, IReadOnlyList<ChangedFile> files, CancellationToken cancellationToken)
	{
		var sources = new List<SourceChange>();
		var (comparisons, unresolved) = await CompareApisAsync(request, upstream, repository, files, cancellationToken);
		foreach (var file in files.OrderBy(file => file.Path, StringComparer.Ordinal))
		{
			var watches = request.Manifest.Targets.Where(watch => Matches(watch, file.Path)).ToArray();
			if (watches.Length == 0)
			{
				continue;
			}
			var changes = comparisons.Where(comparison => Matches(comparison.Key, file.Path))
				.SelectMany(comparison => comparison.Value).ToArray();
			sources.Add(new(file.Path, file.Kind, Classify(watches, changes)));
		}
		var apis = comparisons.Values.SelectMany(changes => changes).ToArray();
		return await ReportAsync(request, upstream, repository, request.BaselineCommit ?? request.Framework.ReviewedCommit,
			files, sources, apis, unresolved, cancellationToken);
	}

	// Silence is a property of each watch, not of the run. A watch the compare already speaks to is
	// reported through its own source change and needs no separate existence check; every other
	// watch is resolved against the reviewed commit, whether or not some unrelated watch drifted in
	// the same run. Gating this on the run having nothing else to report would leave the other 51
	// entries of a 52-entry manifest unchecked for as long as any one of them keeps drifting, which
	// is the #219 failure itself. A watch the compare speaks to is unresolved only when API-surface
	// comparison found no file where it needs one at the target, and arrives here already found.
	private static async Task<MonitorResult> ReportAsync(MonitorRequest request, UpstreamRevision upstream,
		UpstreamRepository repository, string baseline, IReadOnlyList<ChangedFile> files, IReadOnlyList<SourceChange> sources,
		IReadOnlyList<ApiChange> apis, IReadOnlyList<UnresolvedWatch> compared, CancellationToken cancellationToken)
	{
		var unresolved = compared.ToList();
		// Applicability is decided before the collapse. Two entries can share a path and match while
		// differing in scope, so collapsing first would let whichever happened to be listed first
		// answer for the rest, and an inapplicable survivor would drop the path from checking.
		foreach (var watch in request.Manifest.Targets.Where(watch => AppliesTo(watch, request.Framework))
			.DistinctBy(watch => (watch.Path, watch.Match))
			.Where(watch => !files.Any(file => Matches(watch, file.Path)))
			.OrderBy(watch => watch.Path, StringComparer.Ordinal).ThenBy(watch => watch.Match))
		{
			if (await repository.ResolveAsync(watch, baseline, cancellationToken) is { } finding)
			{
				unresolved.Add(new(watch.Path, finding));
			}
		}
		// An unresolved path outranks drift because it is a manifest defect: until it is corrected the
		// monitor cannot speak for that dependency at all. The drift that was found is carried into
		// the same reports rather than discarded, and is reported on its own once the manifest is fixed.
		if (unresolved.Count > 0)
		{
			return MonitorReports.Create(request, MonitorStatus.UnresolvedWatch, upstream, sources, apis, null,
				unresolved.OrderBy(watch => watch.Path, StringComparer.Ordinal).ToArray());
		}
		return MonitorReports.Create(request, sources.Count == 0 ? MonitorStatus.Current : MonitorStatus.Drift, upstream, sources, apis);
	}

	private static bool InApiSurface(WatchTarget watch, string path) => watch.Match == WatchMatch.File ||
		UpstreamRepository.ParentDirectory(path) == UpstreamRepository.ParentDirectory(watch.Path);

	internal static bool Matches(WatchTarget target, string path) => target.Match == WatchMatch.Prefix
		? path.StartsWith(target.Path, StringComparison.Ordinal)
		: path.Equals(target.Path, StringComparison.Ordinal);

	// One answer to "does this watch speak for this framework", shared with ManifestDependencyPolicy
	// so the two cannot drift apart. An absent list means every configured framework; a present one
	// is checked entry by entry, because reading only its first would drop a watch that names this
	// framework later in the list.
	internal static bool AppliesTo(WatchTarget target, FrameworkBaseline framework) =>
		target.Frameworks is null || target.Frameworks.Contains(framework.TargetFramework, StringComparer.Ordinal);

	private static async Task<(Dictionary<WatchTarget, IReadOnlyList<ApiChange>> Comparisons, IReadOnlyList<UnresolvedWatch> Unresolved)>
		CompareApisAsync(MonitorRequest request, UpstreamRevision upstream, UpstreamRepository repository,
			IReadOnlyList<ChangedFile> files, CancellationToken cancellationToken)
	{
		var comparisons = new Dictionary<WatchTarget, IReadOnlyList<ApiChange>>();
		var unresolved = new List<UnresolvedWatch>();
		var watches = request.Manifest.Targets.Where(watch => watch.ApiSurface != ApiSurface.None)
			.DistinctBy(watch => (watch.Path, watch.Match, watch.ApiSurface))
			.OrderBy(watch => watch.Path, StringComparer.Ordinal).ThenBy(watch => watch.Match).ThenBy(watch => watch.ApiSurface);
		foreach (var watch in watches)
		{
			// A prefix watch's API surface is the files its one-directory listing inventories. A changed
			// file beneath a matching subdirectory still reaches the report as source drift through
			// Matches, but that listing cannot contain it, so handing it to ApiSourceAsync would trip the
			// omission guard and fail the whole run. A changed file directly in the directory stays a
			// candidate, so the guard still catches a listing that omits one.
			var matchingFiles = files.Where(file => Matches(watch, file.Path) && InApiSurface(watch, file.Path))
				.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
			if (matchingFiles.Length == 0)
			{
				continue;
			}
			var (changes, finding) = await ApiChangesAsync(request, upstream, repository, watch, matchingFiles, cancellationToken);
			comparisons.Add(watch, changes);
			// Applicability is decided over every entry the collapse merged, as in ReportAsync, so a
			// differently scoped duplicate listed first cannot drop the finding.
			if (finding is { } found && unresolved.All(entry => entry.Path != watch.Path) &&
				request.Manifest.Targets.Any(entry => entry.Path == watch.Path && entry.Match == watch.Match && AppliesTo(entry, request.Framework)))
			{
				unresolved.Add(new(watch.Path, found));
			}
		}
		return (comparisons, unresolved);
	}

	// A symlink or submodule contributes no API source at either revision, and whether the watch is
	// unresolved is decided at the target alone (#244). A watch unresolved at the target is not
	// compared, since it has no API surface there to compare against.
	private static async Task<(IReadOnlyList<ApiChange> Changes, WatchFinding? Finding)> ApiChangesAsync(MonitorRequest request,
		UpstreamRevision upstream, UpstreamRepository repository, WatchTarget watch, ChangedFile[] files, CancellationToken cancellationToken)
	{
		var baseline = await ApiSourceAsync(repository, watch, files, request.BaselineCommit ?? request.Framework.ReviewedCommit,
			ChangeKind.Added, cancellationToken);
		var target = await ApiSourceAsync(repository, watch, files, upstream.Commit, ChangeKind.Removed, cancellationToken);
		return target.Finding is { } finding
			? ([], finding)
			: (ApiSurfaceComparer.Compare(baseline.Source, target.Source, Path.GetFileName(watch.Path).Split('.')[0]), null);
	}

	private static async Task<(string Source, WatchFinding? Finding)> ApiSourceAsync(UpstreamRepository repository, WatchTarget watch,
		ChangedFile[] files, string commit, ChangeKind absentKind, CancellationToken cancellationToken)
	{
		var expected = files.Where(file => file.Kind != absentKind).Select(file => file.Path).ToArray();
		var (paths, finding) = watch.Match == WatchMatch.Prefix
			? await PrefixSourcePathsAsync(repository, watch, commit, expected, cancellationToken)
			: (expected, null);
		var source = new StringBuilder();
		foreach (var path in paths)
		{
			var (text, kind) = await repository.SourceAsync(path, commit, cancellationToken);
			source.AppendLine(text);
			// Only the listing names a prefix watch's finding. A listed file that answers as a
			// symlink or submodule contributes no source, and the watch's real files still decide.
			if (watch.Match == WatchMatch.File)
			{
				finding ??= kind;
			}
		}
		return (source.ToString(), finding);
	}

	// The listing answers which changed paths are symlinks, submodules or directories, and those are
	// not API candidates. A changed path it omits entirely is still an incomplete inventory. A
	// listing with matches but no file among them names the watch by precedence; one with no
	// matches at all is an ordinary removal.
	private static async Task<(IReadOnlyList<string> Paths, WatchFinding? Finding)> PrefixSourcePathsAsync(
		UpstreamRepository repository, WatchTarget watch, string commit, string[] expected, CancellationToken cancellationToken)
	{
		var entries = await repository.PrefixInventoryAsync(watch.Path, commit, cancellationToken);
		var paths = entries.Where(entry => entry.Kind is null).Select(entry => entry.Path).ToArray();
		if (expected.Except(entries.Select(entry => entry.Path), StringComparer.Ordinal).Any())
		{
			throw new MonitorFailure("GitHub directory inventory omitted a changed file known to exist at this revision.");
		}
		return (paths, entries.Count == 0 ? null : UpstreamRepository.PrefixFinding(entries.Select(entry => entry.Kind).ToArray()));
	}

	private static ReviewClassification Classify(WatchTarget[] watches, IReadOnlyList<ApiChange> changes)
	{
		if (watches.Any(watch => watch.Relationship is WatchRelationship.Mirrors or WatchRelationship.Reimplements))
		{
			return ReviewClassification.ParityRequired;
		}
		if (watches.Any(watch => watch.Relationship == WatchRelationship.PrivateAccesses) ||
			changes.Any(change => change.Kind == ChangeKind.Removed))
		{
			return ReviewClassification.CompatibilityRisk;
		}
		return changes.Count > 0 ? ReviewClassification.ExtensibilityOpportunity : ReviewClassification.ImplementationReview;
	}
}
