using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal static class WatchManifestFile
{
	public static WatchManifest Read(string root)
	{
		using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json")));
		var manifest = document.RootElement;
		var frameworks = manifest.TryGetProperty("frameworks", out var configuredFrameworks)
			? configuredFrameworks.EnumerateArray().Select(ReadFramework).ToArray()
			: [ReadLegacyFramework(manifest)];
		return new(manifest.GetProperty("repository").GetString()!, frameworks,
			manifest.GetProperty("watches").EnumerateArray().Select(watch => ReadWatch(watch, frameworks)).ToArray());
	}

	internal static WatchRelationship ParseRelationship(string value) => value.Equals("private-accesses", StringComparison.OrdinalIgnoreCase)
		? WatchRelationship.PrivateAccesses : Enum.Parse<WatchRelationship>(value, true);

	private static WatchTarget ReadWatch(JsonElement watch, IReadOnlyList<FrameworkBaseline> frameworks)
	{
		var path = watch.GetProperty("path").GetString()!;
		return new(path,
			Enum.Parse<WatchMatch>(watch.GetProperty("match").GetString()!, true),
			Enum.Parse<ApiSurface>(watch.GetProperty("api").GetString()!, true),
			ParseRelationship(watch.GetProperty("relationship").GetString()!),
			watch.GetProperty("dependencies").EnumerateArray().Select(value => value.GetString()!).ToArray())
		{
			Frameworks = ReadWatchFrameworks(watch, path, frameworks),
		};
	}

	// An absent list means the watch applies to every configured framework. An empty list names
	// none, which is neither that meaning nor a useful one: read as "no framework" it would stop
	// monitoring the watch without saying so. Both it and an unrecognised name are manifest
	// mistakes, so both fail here rather than becoming a silently unwatched dependency.
	private static IReadOnlyList<string>? ReadWatchFrameworks(JsonElement watch, string path,
		IReadOnlyList<FrameworkBaseline> frameworks)
	{
		if (!watch.TryGetProperty("frameworks", out var declared))
		{
			return null;
		}
		var names = declared.EnumerateArray().Select(value => value.GetString()!).ToArray();
		if (names.Length == 0)
		{
			throw new MonitorFailure($"Watch '{path}' declares an empty frameworks list. Omit the list to watch every configured framework.");
		}
		foreach (var name in names.Where(name => !frameworks.Any(framework =>
			framework.TargetFramework.Equals(name, StringComparison.Ordinal))))
		{
			throw new MonitorFailure($"Watch '{path}' declares framework '{name}', which the manifest does not configure.");
		}
		return names;
	}

	private static FrameworkBaseline ReadFramework(JsonElement framework)
	{
		var reviewed = framework.GetProperty("reviewed");
		return new(framework.GetProperty("targetFramework").GetString()!, framework.GetProperty("majorVersion").GetInt32(),
			framework.GetProperty("allowsPrerelease").GetBoolean(), framework.GetProperty("referencePackVersion").GetString()!,
			reviewed.GetProperty("tag").GetString()!, reviewed.GetProperty("commit").GetString()!);
	}

	private static FrameworkBaseline ReadLegacyFramework(JsonElement manifest)
	{
		var reviewed = manifest.GetProperty("reviewed");
		return new("net10.0", 10, false, "10.0.11", reviewed.GetProperty("tag").GetString()!, reviewed.GetProperty("commit").GetString()!);
	}
}
