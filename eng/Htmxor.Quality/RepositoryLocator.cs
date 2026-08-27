namespace Htmxor.Quality;

internal static class RepositoryLocator
{
	public static string Find()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null && !File.Exists(Path.Combine(current.FullName, "Htmxor.sln")))
		{
			current = current.Parent;
		}

		return current?.FullName
			?? throw new InvalidOperationException("Could not locate Htmxor.sln.");
	}
}
