using System;

namespace Htmxor.Generators;

internal static class RazorImportsFile
{
	private const string FileName = "_Imports.razor";

	// Mapped paths may use either separator, whatever the host OS.
	public static bool Matches(string? path)
		=> path is not null &&
			path.EndsWith(FileName, StringComparison.OrdinalIgnoreCase) &&
			(path.Length == FileName.Length ||
				path[path.Length - FileName.Length - 1] is '/' or '\\');
}
