namespace Htmxor.UpstreamMonitor;

internal static class ManifestDependencyPolicy
{
	public static IReadOnlyList<string> FindMissingDependencies(string repositoryRoot, WatchManifest manifest) =>
		manifest.Targets.SelectMany(target => target.LocalDependencies).Distinct(StringComparer.Ordinal)
			.Where(path => !File.Exists(Path.Combine(repositoryRoot, path))).Order(StringComparer.Ordinal).ToArray();

	// Coverage is decided while each discovery still knows which framework found it. Collapsing
	// the frameworks first would let a watch scoped to one of them answer for all, which is the
	// blindness the scope exists to remove.
	public static IReadOnlyList<LocalFrameworkDependency> FindUntrackedDependencies(string repositoryRoot, WatchManifest manifest) =>
		manifest.Frameworks.SelectMany(framework => LocalFrameworkDependencyDiscovery.Discover(repositoryRoot, framework)
			.Where(dependency => !Covered(manifest, dependency, framework))).Distinct()
			.OrderBy(dependency => dependency.LocalPath, StringComparer.Ordinal).ThenBy(dependency => dependency.UpstreamPath, StringComparer.Ordinal)
			.ThenBy(dependency => dependency.Relationship).ToArray();

	private static bool Covered(WatchManifest manifest, LocalFrameworkDependency dependency, FrameworkBaseline framework) =>
		manifest.Targets.Any(watch => Applies(watch, framework) &&
			UpstreamMonitorApplication.Matches(watch, dependency.UpstreamPath) && watch.Relationship == dependency.Relationship &&
			watch.LocalDependencies.Contains(dependency.LocalPath, StringComparer.Ordinal));

	private static bool Applies(WatchTarget watch, FrameworkBaseline framework) =>
		watch.Frameworks is null || watch.Frameworks.Contains(framework.TargetFramework, StringComparer.Ordinal);
}
