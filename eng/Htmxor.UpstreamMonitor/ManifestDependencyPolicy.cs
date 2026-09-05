namespace Htmxor.UpstreamMonitor;

internal static class ManifestDependencyPolicy
{
	public static IReadOnlyList<string> FindMissingDependencies(
		string repositoryRoot,
		WatchManifest manifest)
	{
		_ = repositoryRoot;
		_ = manifest;
		return [];
	}
}
