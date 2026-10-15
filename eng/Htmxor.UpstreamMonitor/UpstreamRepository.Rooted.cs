using System.Text.Json;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class UpstreamRepository
{
	private async Task<IReadOnlyList<ChangedFile>> CompareRootedAsync(string baseline, string target,
		CancellationToken cancellationToken)
	{
		var before = await RootedEntriesAsync(baseline, cancellationToken);
		var after = await RootedEntriesAsync(target, cancellationToken);
		var changes = new List<ChangedFile>();
		foreach (var path in before.Keys.Union(after.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
		{
			before.TryGetValue(path, out var oldEntry);
			after.TryGetValue(path, out var newEntry);
			oldEntry = oldEntry?.Type == "tree" ? null : oldEntry;
			newEntry = newEntry?.Type == "tree" ? null : newEntry;
			if (oldEntry == newEntry)
			{
				continue;
			}
			changes.Add(new(path, oldEntry is null ? ChangeKind.Added : newEntry is null ? ChangeKind.Removed : ChangeKind.Changed));
		}
		return changes;
	}

	private async Task<Dictionary<string, TreeEntry>> RootedEntriesAsync(string commit, CancellationToken cancellationToken)
	{
		var sha = commit;
		var commitReference = true;
		foreach (var segment in manifest.SourceRoot!.Split('/'))
		{
			var entries = await ReadTreeAsync(sha, recursive: false, cancellationToken, commitReference);
			if (!entries.TryGetValue(segment, out var directory) || directory.Type != "tree")
			{
				throw new MonitorFailure("GitHub source root is missing or is not a directory.");
			}
			sha = directory.Sha;
			commitReference = false;
		}
		return await ReadTreeAsync(sha, recursive: true, cancellationToken, commitReference: false);
	}

	private async Task<Dictionary<string, TreeEntry>> ReadTreeAsync(string sha, bool recursive,
		CancellationToken cancellationToken, bool commitReference)
	{
		var suffix = recursive ? "?recursive=1" : "";
		var response = await api.GetAsync($"/repos/{repository}/git/trees/{Uri.EscapeDataString(sha)}{suffix}", cancellationToken);
		var tree = TreeArray(response, sha, commitReference);
		var entries = new Dictionary<string, TreeEntry>(StringComparer.Ordinal);
		foreach (var value in tree.EnumerateArray())
		{
			var entry = ParseTreeEntry(value, recursive);
			if (!entries.TryAdd(entry.Path, entry))
			{
				throw new MonitorFailure("GitHub source tree repeated a path.");
			}
		}
		if (recursive && entries.Keys.Any(path => MissingTreeParent(entries, path)))
		{
			throw new MonitorFailure("GitHub source tree omitted a parent directory.");
		}
		return entries;
	}

	private static JsonElement TreeArray(JsonElement response, string sha, bool commitReference)
	{
		if (response.ValueKind != JsonValueKind.Object ||
			!response.TryGetProperty("truncated", out var truncated) || truncated.ValueKind != JsonValueKind.False ||
			!response.TryGetProperty("tree", out var tree) || tree.ValueKind != JsonValueKind.Array ||
			!ValidTreeIdentity(response, sha, commitReference))
		{
			throw new MonitorFailure("GitHub source tree is missing, truncated or malformed.");
		}
		return tree;
	}

	private static bool ValidTreeIdentity(JsonElement response, string sha, bool commitReference) =>
		response.TryGetProperty("sha", out var identity) && identity.ValueKind == JsonValueKind.String &&
		identity.GetString() is { } resolved && resolved.Length == 40 && resolved.All(Uri.IsHexDigit) &&
		(commitReference || resolved == sha);

	private static bool MissingTreeParent(Dictionary<string, TreeEntry> entries, string path)
	{
		var parent = ParentDirectory(path);
		return parent.Length > 0 && (!entries.TryGetValue(parent, out var directory) || directory.Type != "tree");
	}

	private static TreeEntry ParseTreeEntry(JsonElement value, bool recursive)
	{
		if (value.ValueKind != JsonValueKind.Object || !TreeString(value, "path", out var path) ||
			!TreeString(value, "type", out var type) || !TreeString(value, "sha", out var sha) ||
			!TreeString(value, "mode", out var mode) || !SafeTreePath(path, recursive) ||
			sha.Length != 40 || !sha.All(Uri.IsHexDigit) || !ValidTreeKind(type, mode))
		{
			throw new MonitorFailure("GitHub source tree contained an invalid entry.");
		}
		return new(path, type, sha, mode);
	}

	private static bool TreeString(JsonElement value, string name, out string text)
	{
		text = value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
			? property.GetString() ?? "" : "";
		return text.Length > 0;
	}

	private static bool SafeTreePath(string path, bool recursive) =>
		(recursive || !path.Contains('/')) && path.Split('/').All(segment =>
			segment.Length > 0 && segment is not ("." or "..") && !segment.Any(character =>
				char.IsControl(character) || character is '\\' or '?' or '#'));

	private static bool ValidTreeKind(string type, string mode) => type switch
	{
		"tree" => mode == "040000" || mode == "40000",
		"blob" => mode is "100644" or "100755" or "120000",
		"commit" => mode == "160000",
		_ => false,
	};

	private sealed record TreeEntry(string Path, string Type, string Sha, string Mode);
}
