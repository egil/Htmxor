namespace Htmxor.Quality.Tests;

internal sealed class TemporaryDirectory : IDisposable
{
	public TemporaryDirectory()
	{
		Path = System.IO.Path.Combine(
			System.IO.Path.GetTempPath(),
			"htmxor-verification-tests",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path);
	}

	public string Path { get; }

	public void Dispose()
	{
		if (Directory.Exists(Path))
		{
			Directory.Delete(Path, recursive: true);
		}
	}
}
