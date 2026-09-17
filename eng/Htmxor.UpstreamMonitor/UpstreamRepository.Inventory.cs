using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class UpstreamRepository
{
	private const string InvalidInventory = "GitHub directory inventory is invalid or reached the 1000-entry limit; completeness is unknown.";

	// A prefix watch resolves when the directory holding it exists and contains at least one file
	// under the prefix; a file watch resolves when the contents API can see it. A missing directory
	// answers the same question as an empty match, so it is a non-resolving watch rather than an
	// infrastructure failure.
	public async Task<bool> ResolvesAsync(WatchTarget watch, string commit, CancellationToken cancellationToken)
	{
		if (watch.Match == WatchMatch.File)
		{
			return await api.ExistsAsync(ContentsPath(watch.Path, commit), cancellationToken);
		}
		var separator = watch.Path.LastIndexOf('/');
		var directory = separator < 0 ? "" : watch.Path[..separator];
		if (!await api.ExistsAsync(ContentsPath(directory, commit), cancellationToken))
		{
			return false;
		}
		return (await PrefixSourcePathsAsync(watch.Path, commit, cancellationToken)).Count > 0;
	}

	public async Task<IReadOnlyList<string>> PrefixSourcePathsAsync(string prefix, string commit, CancellationToken cancellationToken)
	{
		var separator = prefix.LastIndexOf('/');
		var directory = separator < 0 ? "" : prefix[..separator];
		var listing = await api.GetAsync(ContentsPath(directory, commit), cancellationToken);
		if (listing.ValueKind != JsonValueKind.Array || listing.GetArrayLength() >= 1000)
		{
			throw new MonitorFailure(InvalidInventory);
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
		var path = RequiredString(entry, "path");
		var parent = directory.Length == 0 ? "" : directory + "/";
		if (string.IsNullOrEmpty(path) || !path.StartsWith(parent, StringComparison.Ordinal) || path[parent.Length..].Contains('/'))
		{
			throw new MonitorFailure("GitHub directory inventory contained a path outside the requested directory.");
		}
		return path;
	}

	private static bool IsFile(JsonElement entry) => RequiredString(entry, "type") switch
	{
		"file" => true,
		"dir" or "symlink" or "submodule" => false,
		_ => throw new MonitorFailure("GitHub directory inventory contained an unsupported entry type."),
	};

	private static string RequiredString(JsonElement entry, string name)
	{
		if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty(name, out var value) ||
			value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
		{
			throw new MonitorFailure(InvalidInventory);
		}
		return value.GetString()!;
	}
}
