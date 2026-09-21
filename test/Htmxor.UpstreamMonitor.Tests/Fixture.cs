using System.Text;
using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

internal static class Fixture
{
	public const string Repository = "dotnet/aspnetcore";
	public const string ReviewedCommit = "a5383385245bdacc20ec19f30e46090a8154d8da";
	public const string Net11ReviewedTag = "v11.0.0-rc.1.26425.128";
	public const string Net11ReviewedCommit = "c3325eeb6b47bc6383c127d4f4827dc9642a2b6e";
	public const string TargetCommit = "cccccccccccccccccccccccccccccccccccccccc";
	public const string BaselineCommit = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

	public static string Read(string relativePath) =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath));

	public static string GitHubContent(string relativeSourcePath)
		=> GitHubContentText(Read(relativeSourcePath));

	public static string GitHubContentText(string source)
	{
		return JsonSerializer.Serialize(new
		{
			type = "file",
			encoding = "base64",
			content = Convert.ToBase64String(Encoding.UTF8.GetBytes(source)),
		});
	}

	public static WatchManifest Manifest(params WatchTarget[] targets) =>
		new(Repository, "v10.0.11", ReviewedCommit, targets);

	public static WatchManifest ManifestFor(FrameworkBaseline framework, params WatchTarget[] targets) =>
		new(Repository, [framework], targets);

	public static WatchManifest MultiTargetManifest(params WatchTarget[] targets) =>
		new(Repository, [Net10Framework(), Net11Framework()], targets);

	public static FrameworkBaseline Net10Framework() =>
		new("net10.0", 10, false, "10.0.11", "v10.0.11", ReviewedCommit);

	public static FrameworkBaseline Net11Framework() =>
		new("net11.0", 11, true, "11.0.0-rc.1.26425.128", Net11ReviewedTag, Net11ReviewedCommit);

	public static WatchTarget Watch(
		string path,
		WatchMatch match = WatchMatch.File,
		ApiSurface apiSurface = ApiSurface.None,
		WatchRelationship relationship = WatchRelationship.Reimplements,
		params string[] dependencies) =>
		new(path, match, apiSurface, relationship, dependencies);

	public static UpstreamMonitorApplication Application(FakeGitHubTransport transport)
	{
		var client = new HttpClient(transport)
		{
			BaseAddress = new Uri("https://api.github.test"),
		};
		return new UpstreamMonitorApplication(client);
	}
}

/// <summary>
/// A temporary repository whose local dependency source carries a
/// <c>TARGET_FRAMEWORK_SYMBOL</c> placeholder, substituted with the requested preprocessor
/// symbol so tests can drive <see cref="LocalFrameworkDependencyDiscovery"/> under different
/// active symbols without a real multi-target compile.
/// </summary>
internal sealed class ConditionalRepository : IDisposable
{
	public ConditionalRepository(string fixture, string symbol = "")
	{
		Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"htmxor-target-framework-{Guid.NewGuid():N}");
		var destination = System.IO.Path.Combine(Path, "src", "Htmxor");
		Directory.CreateDirectory(destination);
		foreach (var source in Directory.EnumerateFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "target-framework-dependencies", fixture), "*.cs"))
		{
			File.WriteAllText(System.IO.Path.Combine(destination, System.IO.Path.GetFileName(source)),
				File.ReadAllText(source).Replace("TARGET_FRAMEWORK_SYMBOL", symbol, StringComparison.Ordinal));
		}
	}

	public string Path { get; }

	public void Dispose() => Directory.Delete(Path, recursive: true);
}

/// <summary>
/// A temporary repository for writing arbitrary local source and manifest files.
/// </summary>
internal sealed class TemporaryRepository : IDisposable
{
	public TemporaryRepository()
	{
		Path = System.IO.Path.Combine(
			System.IO.Path.GetTempPath(),
			$"htmxor-upstream-monitor-{Guid.NewGuid():N}");
		Directory.CreateDirectory(Path);
	}

	public string Path { get; }

	public void WriteFixture(string fixture)
	{
		var directory = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "dependencies", fixture);
		foreach (var file in Directory.GetFiles(directory, "*.cs"))
		{
			Write($"src/Htmxor/{System.IO.Path.GetFileName(file)}", File.ReadAllText(file));
		}
	}

	public void Write(string relativePath, string contents = "")
	{
		var path = System.IO.Path.Combine(Path, relativePath);
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
		File.WriteAllText(path, contents);
	}

	public void Dispose() => Directory.Delete(Path, recursive: true);
}
