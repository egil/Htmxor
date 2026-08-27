namespace Htmxor.Quality;

internal static class ArtifactDirectory
{
	public static string Reset(string repositoryRoot, string profile)
	{
		var repositoryPath = Path.GetFullPath(repositoryRoot);
		var artifactsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "artifacts", "results"));
		var path = Path.GetFullPath(Path.Combine(artifactsRoot, profile));
		EnsureChildPath(artifactsRoot, path);
		EnsureOrdinaryPath(repositoryPath, path);
		if (Directory.Exists(path))
		{
			EnsureNoReparsePoints(path);
			Directory.Delete(path, recursive: true);
		}

		EnsureOrdinaryPath(repositoryPath, path);
		Directory.CreateDirectory(path);
		EnsureOrdinaryPath(repositoryPath, path);
		return path;
	}

	private static void EnsureChildPath(string root, string path)
	{
		var prefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
		if (!path.StartsWith(prefix, PathComparison))
		{
			throw new InvalidOperationException($"Refusing to reset artifact path '{path}'.");
		}
	}

	private static void EnsureOrdinaryPath(string root, string path)
	{
		EnsureChildPath(root, path);
		RequireOrdinaryEntryIfPresent(root);
		var current = root;
		foreach (var segment in Path.GetRelativePath(root, path).Split(
			[Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
			StringSplitOptions.RemoveEmptyEntries))
		{
			current = Path.Combine(current, segment);
			RequireOrdinaryEntryIfPresent(current);
		}
	}

	private static void EnsureNoReparsePoints(string root)
	{
		var pending = new Stack<string>();
		pending.Push(root);
		while (pending.TryPop(out var directory))
		{
			RequireOrdinaryEntry(directory);
			foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
			{
				RequireOrdinaryEntry(entry);
				if (File.GetAttributes(entry).HasFlag(FileAttributes.Directory))
				{
					pending.Push(entry);
				}
			}
		}
	}

	private static void RequireOrdinaryEntry(string path)
	{
		if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
		{
			throw new InvalidOperationException(
				$"Refusing to reset artifact directory containing reparse point '{path}'.");
		}
	}

	private static void RequireOrdinaryEntryIfPresent(string path)
	{
		if (Directory.Exists(path) || File.Exists(path))
		{
			RequireOrdinaryEntry(path);
		}
	}

	private static StringComparison PathComparison =>
		OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
