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
			manifest.GetProperty("watches").EnumerateArray().Select(ReadWatch).ToArray());
	}

	internal static WatchRelationship ParseRelationship(string value) => value.Equals("private-accesses", StringComparison.OrdinalIgnoreCase)
		? WatchRelationship.PrivateAccesses : Enum.Parse<WatchRelationship>(value, true);

	private static WatchTarget ReadWatch(JsonElement watch) => new(watch.GetProperty("path").GetString()!,
		Enum.Parse<WatchMatch>(watch.GetProperty("match").GetString()!, true),
		Enum.Parse<ApiSurface>(watch.GetProperty("api").GetString()!, true),
		ParseRelationship(watch.GetProperty("relationship").GetString()!),
		watch.GetProperty("dependencies").EnumerateArray().Select(value => value.GetString()!).ToArray());

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
