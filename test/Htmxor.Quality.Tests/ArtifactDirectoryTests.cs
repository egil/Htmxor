using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class ArtifactDirectoryTests
{
	[Fact]
	public void Reset_rejects_a_linked_artifacts_parent_without_touching_its_target()
	{
		using var external = new TemporaryDirectory();
		using var repository = new TemporaryDirectory();
		var marker = Path.Combine(external.Path, "marker.txt");
		var link = Path.Combine(repository.Path, "artifacts");
		File.WriteAllText(marker, "preserve");
		CreateDirectoryLink(link, external.Path);
		try
		{
			var exception = Assert.Throws<InvalidOperationException>(
				() => ArtifactDirectory.Reset(repository.Path, "fast"));

			Assert.Contains("reparse point", exception.Message, StringComparison.Ordinal);
			Assert.True(File.Exists(marker));
		}
		finally
		{
			Directory.Delete(link, recursive: false);
		}
	}

	private static void CreateDirectoryLink(string link, string target)
	{
		if (!OperatingSystem.IsWindows())
		{
			Directory.CreateSymbolicLink(link, target);
			return;
		}

		var startInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe")
		{
			CreateNoWindow = true,
			UseShellExecute = false,
		};
		foreach (var argument in new[] { "/d", "/c", "mklink", "/J", link, target })
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = System.Diagnostics.Process.Start(startInfo)
			?? throw new InvalidOperationException("Could not start mklink.");
		process.WaitForExit();
		Assert.Equal(0, process.ExitCode);
	}
}
