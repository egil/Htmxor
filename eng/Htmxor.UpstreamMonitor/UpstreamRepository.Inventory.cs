using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class UpstreamRepository
{
	private const string InvalidInventory = "GitHub directory inventory is invalid or reached the 1000-entry limit; completeness is unknown.";

	// A file watch resolves only to a file and a prefix watch only to a directory holding at least
	// one matching file. The contents API answers 200 for both kinds, so status alone would report a
	// file watch aimed at a directory as resolved, and the mistake would surface much later as
	// SourceAsync failing to find the file body it assumes. A missing path answers the same question
	// as an empty match, so both are non-resolving watches rather than infrastructure failures.
	public async Task<bool> ResolvesAsync(WatchTarget watch, string commit, CancellationToken cancellationToken)
	{
		if (watch.Match == WatchMatch.File)
		{
			var content = await api.TryGetAsync(ContentsPath(watch.Path, commit), cancellationToken);
			return content is { ValueKind: JsonValueKind.Object } file && IsFile(file);
		}
		var listing = await api.TryGetAsync(ContentsPath(ParentDirectory(watch.Path), commit), cancellationToken);
		return listing is { ValueKind: JsonValueKind.Array } entries && PrefixSourcePaths(entries, watch.Path).Count > 0;
	}

	public async Task<IReadOnlyList<string>> PrefixSourcePathsAsync(string prefix, string commit, CancellationToken cancellationToken)
	{
		var listing = await api.GetAsync(ContentsPath(ParentDirectory(prefix), commit), cancellationToken);
		return PrefixSourcePaths(listing, prefix);
	}

	// A prefix watch names a file-name stem inside one directory, because the listing that
	// inventories it is not recursive. A prefix that names a directory would be compared against
	// every file beneath it while none of them is inventoried, so it is a manifest error rather
	// than a watch that does or does not resolve. A sibling directory sharing the stem is unrelated.
	private static IReadOnlyList<string> PrefixSourcePaths(JsonElement listing, string prefix)
	{
		if (listing.ValueKind != JsonValueKind.Array || listing.GetArrayLength() >= 1000)
		{
			throw new MonitorFailure(InvalidInventory);
		}
		var directory = ParentDirectory(prefix);
		var paths = new SortedSet<string>(StringComparer.Ordinal);
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (var entry in listing.EnumerateArray())
		{
			var path = EntryPath(entry, directory);
			var type = EntryType(entry);
			if (!seen.Add(path))
			{
				throw new MonitorFailure("GitHub directory inventory repeated a path.");
			}
			if (type == "dir" && path.Equals(prefix, StringComparison.Ordinal))
			{
				throw new MonitorFailure($"Prefix watch '{prefix}' names an upstream directory. A prefix watch names a file-name stem inside one directory.");
			}
			if (type == "file" && path.StartsWith(prefix, StringComparison.Ordinal))
			{
				paths.Add(path);
			}
		}
		return paths.ToArray();
	}

	private static string ParentDirectory(string path)
	{
		var separator = path.LastIndexOf('/');
		return separator < 0 ? "" : path[..separator];
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

	private static bool IsFile(JsonElement entry) => EntryType(entry) == "file";

	private static string EntryType(JsonElement entry)
	{
		var type = RequiredString(entry, "type");
		return type is "file" or "dir" or "symlink" or "submodule"
			? type
			: throw new MonitorFailure("GitHub directory inventory contained an unsupported entry type.");
	}

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
