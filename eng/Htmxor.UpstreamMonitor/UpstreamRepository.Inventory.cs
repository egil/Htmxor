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
	// Returns null when the watch resolves, otherwise what was found at its path.
	public async Task<WatchFinding?> ResolveAsync(WatchTarget watch, string commit, CancellationToken cancellationToken)
	{
		if (watch.Match == WatchMatch.File)
		{
			// GitHub answers a directory with an array and a file, symlink or submodule with an object.
			return await api.TryGetAsync(ContentsPath(watch.Path, commit), cancellationToken) switch
			{
				{ ValueKind: JsonValueKind.Array } => WatchFinding.Directory,
				{ ValueKind: JsonValueKind.Object } content => EntryKind(content),
				_ => WatchFinding.DoesNotExist,
			};
		}
		var listing = await api.TryGetAsync(ContentsPath(ParentDirectory(watch.Path), commit), cancellationToken);
		if (listing is not { ValueKind: JsonValueKind.Array } entries)
		{
			return WatchFinding.DoesNotExist;
		}
		var kinds = PrefixEntries(entries, watch.Path).Select(entry => entry.Kind).ToArray();
		// Any matching file resolves the watch. Otherwise the first kind present in this order names it.
		return kinds.Length == 0 ? WatchFinding.DoesNotExist
			: kinds.Contains(null) ? null
			: new[] { WatchFinding.Directory, WatchFinding.Symlink, WatchFinding.Submodule }.First(kind => kinds.Contains(kind));
	}

	public async Task<IReadOnlyList<string>> PrefixSourcePathsAsync(string prefix, string commit, CancellationToken cancellationToken)
	{
		var listing = await api.GetAsync(ContentsPath(ParentDirectory(prefix), commit), cancellationToken);
		return PrefixSourcePaths(listing, prefix);
	}

	// Only file entries count. The listing is not recursive, so a directory matching the prefix is
	// not inventoried and cannot answer for the files beneath it.
	private static IReadOnlyList<string> PrefixSourcePaths(JsonElement listing, string prefix) =>
		PrefixEntries(listing, prefix).Where(entry => entry.Kind is null).Select(entry => entry.Path).ToArray();

	// Every entry of one directory listing whose path starts with the prefix, with null for a file
	// and otherwise the kind it is. The listing is validated whole, not only its matching entries.
	private static IReadOnlyList<(string Path, WatchFinding? Kind)> PrefixEntries(JsonElement listing, string prefix)
	{
		if (listing.ValueKind != JsonValueKind.Array || listing.GetArrayLength() >= 1000)
		{
			throw new MonitorFailure(InvalidInventory);
		}
		var directory = ParentDirectory(prefix);
		var entries = new SortedDictionary<string, WatchFinding?>(StringComparer.Ordinal);
		foreach (var entry in listing.EnumerateArray())
		{
			var path = EntryPath(entry, directory);
			var kind = EntryKind(entry);
			if (!entries.TryAdd(path, kind))
			{
				throw new MonitorFailure("GitHub directory inventory repeated a path.");
			}
		}
		return entries.Where(entry => entry.Key.StartsWith(prefix, StringComparison.Ordinal))
			.Select(entry => (entry.Key, entry.Value)).ToArray();
	}

	internal static string ParentDirectory(string path)
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

	private static WatchFinding? EntryKind(JsonElement entry) => RequiredString(entry, "type") switch
	{
		"file" => null,
		"dir" => WatchFinding.Directory,
		"symlink" => WatchFinding.Symlink,
		"submodule" => WatchFinding.Submodule,
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
