namespace Htmxor.UpstreamMonitor;

internal static class ManifestDependencyPolicy
{
	public static IReadOnlyList<string> FindMissingDependencies(string repositoryRoot, WatchManifest manifest) =>
		manifest.Targets.SelectMany(target => target.LocalDependencies).Distinct(StringComparer.Ordinal)
			.Where(path => !File.Exists(Path.Combine(repositoryRoot, path))).Order(StringComparer.Ordinal).ToArray();

	public static IReadOnlyList<LocalFrameworkDependency> FindUntrackedDependencies(string repositoryRoot, WatchManifest manifest) =>
		LocalFrameworkDependencyDiscovery.Discover(repositoryRoot).Distinct().Where(dependency => !Covered(manifest, dependency))
			.OrderBy(dependency => dependency.LocalPath, StringComparer.Ordinal).ThenBy(dependency => dependency.UpstreamPath, StringComparer.Ordinal)
			.ThenBy(dependency => dependency.Relationship).ToArray();

	private static bool Covered(WatchManifest manifest, LocalFrameworkDependency dependency) => manifest.Targets.Any(watch =>
		UpstreamMonitorApplication.Matches(watch, dependency.UpstreamPath) && watch.Relationship == dependency.Relationship &&
		watch.LocalDependencies.Contains(dependency.LocalPath, StringComparer.Ordinal));
}
