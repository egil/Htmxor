using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PackedConsumerCollection : ICollectionFixture<PackedPackageFixture>
{
	public const string Name = "Packed package consumers";
}

public sealed class PackedPackageFixture : IAsyncLifetime
{
	private readonly TemporaryDirectory directory = new();

	public string PackageDirectory => directory.Path;

	public string PackageVersion { get; } = $"1.0.0-issue363-{Guid.NewGuid():N}";

	public string PackagePath => Assert.Single(Directory.EnumerateFiles(PackageDirectory, "*.nupkg"));

	public async Task InitializeAsync()
	{
		var root = RepositoryLocator.Find();
		var result = await new ProcessRunner().RunAsync(new(
			"dotnet", root,
			[
				"pack", Path.Combine(root, "src", "Htmxor", "Htmxor.csproj"),
				"--configuration", "Release", "--no-restore", "--output", PackageDirectory,
				$"-p:MinVerVersionOverride={PackageVersion}",
			], EnsureSuccess: false));
		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError);
	}

	public Task DisposeAsync()
	{
		directory.Dispose();
		return Task.CompletedTask;
	}
}
