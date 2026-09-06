using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class UpstreamRepository
{
	public async Task<IReadOnlyList<string>> PrefixSourcePathsAsync(string prefix, string commit, CancellationToken cancellationToken)
	{
		var separator = prefix.LastIndexOf('/');
		var directory = separator < 0 ? "" : prefix[..separator];
		var listing = await api.GetAsync(ContentsPath(directory, commit), cancellationToken);
		if (listing.ValueKind != JsonValueKind.Array || listing.GetArrayLength() >= 1000)
		{
			throw new MonitorFailure("GitHub directory inventory is invalid or reached the 1000-entry limit; completeness is unknown.");
		}
		var paths = new SortedSet<string>(StringComparer.Ordinal);
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var entry in listing.EnumerateArray())
		{
			var path = EntryPath(entry, directory);
			if (!seen.Add(path))
			{
				throw new MonitorFailure("GitHub directory inventory repeated a path.");
			}
			if (IsFile(entry) && path.StartsWith(prefix, StringComparison.Ordinal))
			{
				paths.Add(path);
			}
		}
		return paths.ToArray();
	}

	private static string EntryPath(JsonElement entry, string directory)
	{
		var path = entry.GetProperty("path").GetString();
		var parent = directory.Length == 0 ? "" : directory + "/";
		if (string.IsNullOrEmpty(path) || !path.StartsWith(parent, StringComparison.Ordinal) || path[parent.Length..].Contains('/'))
		{
			throw new MonitorFailure("GitHub directory inventory contained a path outside the requested directory.");
		}
		return path;
	}

	private static bool IsFile(JsonElement entry) => entry.GetProperty("type").GetString() switch
	{
		"file" => true,
		"dir" or "symlink" or "submodule" => false,
		_ => throw new MonitorFailure("GitHub directory inventory contained an unsupported entry type."),
	};
}
