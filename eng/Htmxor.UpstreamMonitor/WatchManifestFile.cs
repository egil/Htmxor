using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal static class WatchManifestFile
{
	public static WatchManifest Read(string root)
	{
		using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "Htmxor.UpstreamMonitor", "upstream-watch.json")));
		var manifest = document.RootElement;
		var reviewed = manifest.GetProperty("reviewed");
		return new(manifest.GetProperty("repository").GetString()!, reviewed.GetProperty("tag").GetString()!,
			reviewed.GetProperty("commit").GetString()!, manifest.GetProperty("watches").EnumerateArray().Select(ReadWatch).ToArray());
	}

	internal static WatchRelationship ParseRelationship(string value) => value == "private-accesses"
		? WatchRelationship.PrivateAccesses : Enum.Parse<WatchRelationship>(value, true);

	private static WatchTarget ReadWatch(JsonElement watch) => new(watch.GetProperty("path").GetString()!,
		Enum.Parse<WatchMatch>(watch.GetProperty("match").GetString()!, true),
		Enum.Parse<ApiSurface>(watch.GetProperty("api").GetString()!, true),
		ParseRelationship(watch.GetProperty("relationship").GetString()!),
		watch.GetProperty("dependencies").EnumerateArray().Select(value => value.GetString()!).ToArray());
}
