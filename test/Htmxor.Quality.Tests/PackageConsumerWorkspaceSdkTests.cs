using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

public sealed class PackageConsumerWorkspaceSdkTests
{
	[Fact]
	public async Task Default_consumer_workspace_uses_the_repository_pinned_SDK()
	{
		using var repository = new TemporaryDirectory();
		Directory.CreateDirectory(Path.Combine(
			repository.Path,
			"test",
			"Htmxor.Quality.Tests",
			"PackageConsumer"));
		File.WriteAllText(Path.Combine(repository.Path, "global.json"),
			"""{"sdk":{"version":"10.0.400","rollForward":"disable"}}""");

		using var workspace = new PackageConsumerWorkspace(repository.Path);
		var result = await new ProcessRunner().RunAsync(new(
			"dotnet",
			workspace.ConsumerDirectory,
			["--version"],
			EnsureSuccess: false));

		Assert.Equal(0, result.ExitCode);
		Assert.Equal("10.0.400", result.StandardOutput.Trim());
	}
}
