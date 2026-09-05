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

	private static WatchTarget ReadWatch(JsonElement watch) => new(watch.GetProperty("path").GetString()!,
		Enum.Parse<WatchMatch>(watch.GetProperty("match").GetString()!, true),
		Enum.Parse<ApiSurface>(watch.GetProperty("api").GetString()!, true),
		Enum.Parse<WatchRelationship>(watch.GetProperty("relationship").GetString()!, true),
		watch.GetProperty("dependencies").EnumerateArray().Select(value => value.GetString()!).ToArray());
}
