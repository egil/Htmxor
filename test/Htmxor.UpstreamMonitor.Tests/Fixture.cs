using System.Text;
using System.Text.Json;
using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

internal static class Fixture
{
	public const string Repository = "dotnet/aspnetcore";
	public const string ReviewedCommit = "a5383385245bdacc20ec19f30e46090a8154d8da";
	public const string TargetCommit = "cccccccccccccccccccccccccccccccccccccccc";
	public const string BaselineCommit = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

	public static string Read(string relativePath) =>
		File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath));

	public static string GitHubContent(string relativeSourcePath)
	{
		var source = Read(relativeSourcePath);
		return JsonSerializer.Serialize(new
		{
			type = "file",
			encoding = "base64",
			content = Convert.ToBase64String(Encoding.UTF8.GetBytes(source)),
		});
	}

	public static WatchManifest Manifest(params WatchTarget[] targets) =>
		new(Repository, "v10.0.11", ReviewedCommit, targets);

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
