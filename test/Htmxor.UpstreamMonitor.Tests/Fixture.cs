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
