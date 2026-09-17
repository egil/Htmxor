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
				return await ReportAsync(request, upstream, repository, baseline, [], [], [], cancellationToken);
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
		var comparisons = await CompareApisAsync(request, upstream, repository, files, cancellationToken);
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
			files, sources, apis, cancellationToken);
	}

	// Silence is a property of each watch, not of the run. A watch that appeared in the changed-file
	// list is proven to exist by the diff itself; every other watch is resolved against the reviewed
	// commit, whether or not some unrelated watch drifted in the same run. Gating this on the run
	// having nothing else to report would leave the other 51 entries of a 52-entry manifest
	// unchecked for as long as any one of them keeps drifting, which is the #219 failure itself.
	private static async Task<MonitorResult> ReportAsync(MonitorRequest request, UpstreamRevision upstream,
		UpstreamRepository repository, string baseline, IReadOnlyList<ChangedFile> files,
		IReadOnlyList<SourceChange> sources, IReadOnlyList<ApiChange> apis, CancellationToken cancellationToken)
	{
		var unresolved = new SortedSet<string>(StringComparer.Ordinal);
		foreach (var watch in request.Manifest.Targets.DistinctBy(watch => (watch.Path, watch.Match))
			.Where(watch => !files.Any(file => Matches(watch, file.Path)))
			.OrderBy(watch => watch.Path, StringComparer.Ordinal).ThenBy(watch => watch.Match))
		{
			if (!await repository.ResolvesAsync(watch, baseline, cancellationToken))
			{
				unresolved.Add(watch.Path);
			}
		}
		// An unresolved path outranks drift because it is a manifest defect: until it is corrected the
		// monitor cannot speak for that dependency at all. The drift that was found is carried into
		// the same reports rather than discarded, and is reported on its own once the manifest is fixed.
		if (unresolved.Count > 0)
		{
			return MonitorReports.Create(request, MonitorStatus.UnresolvedWatch, upstream, sources, apis, null, unresolved.ToArray());
		}
		return MonitorReports.Create(request, sources.Count == 0 ? MonitorStatus.Current : MonitorStatus.Drift, upstream, sources, apis);
	}

	internal static bool Matches(WatchTarget target, string path) => target.Match == WatchMatch.Prefix
		? path.StartsWith(target.Path, StringComparison.Ordinal)
		: path.Equals(target.Path, StringComparison.Ordinal);

	private static async Task<Dictionary<WatchTarget, IReadOnlyList<ApiChange>>> CompareApisAsync(MonitorRequest request,
		UpstreamRevision upstream, UpstreamRepository repository, IReadOnlyList<ChangedFile> files, CancellationToken cancellationToken)
	{
		var comparisons = new Dictionary<WatchTarget, IReadOnlyList<ApiChange>>();
		var watches = request.Manifest.Targets.Where(watch => watch.ApiSurface != ApiSurface.None)
			.DistinctBy(watch => (watch.Path, watch.Match, watch.ApiSurface))
			.OrderBy(watch => watch.Path, StringComparer.Ordinal).ThenBy(watch => watch.Match).ThenBy(watch => watch.ApiSurface);
		foreach (var watch in watches)
		{
			var matchingFiles = files.Where(file => Matches(watch, file.Path)).OrderBy(file => file.Path, StringComparer.Ordinal).ToArray();
			if (matchingFiles.Length > 0)
			{
				comparisons.Add(watch, await ApiChangesAsync(request, upstream, repository, watch, matchingFiles, cancellationToken));
			}
		}
		return comparisons;
	}

	private static async Task<IReadOnlyList<ApiChange>> ApiChangesAsync(MonitorRequest request, UpstreamRevision upstream,
		UpstreamRepository repository, WatchTarget watch, ChangedFile[] files, CancellationToken cancellationToken)
	{
		var baseline = await ApiSourceAsync(repository, watch, files, request.BaselineCommit ?? request.Framework.ReviewedCommit,
			ChangeKind.Added, cancellationToken);
		var target = await ApiSourceAsync(repository, watch, files, upstream.Commit, ChangeKind.Removed, cancellationToken);
		return ApiSurfaceComparer.Compare(baseline, target, Path.GetFileName(watch.Path).Split('.')[0]);
	}

	private static async Task<string> ApiSourceAsync(UpstreamRepository repository, WatchTarget watch, ChangedFile[] files,
		string commit, ChangeKind absentKind, CancellationToken cancellationToken)
	{
		var expected = files.Where(file => file.Kind != absentKind).Select(file => file.Path).ToArray();
		var paths = watch.Match == WatchMatch.Prefix
			? await repository.PrefixSourcePathsAsync(watch.Path, commit, cancellationToken)
			: expected;
		if (expected.Except(paths, StringComparer.Ordinal).Any())
		{
			throw new MonitorFailure("GitHub directory inventory omitted a changed file known to exist at this revision.");
		}
		var source = new StringBuilder();
		foreach (var path in paths)
		{
			source.AppendLine(await repository.SourceAsync(path, commit, cancellationToken));
		}
		return source.ToString();
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
