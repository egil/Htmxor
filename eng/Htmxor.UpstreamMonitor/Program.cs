using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Htmxor.UpstreamMonitor;

internal static class Program
{
	internal static Func<HttpClient> CreateHttpClient { get; set; } = () => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
	{
		BaseAddress = new Uri("https://api.github.com"),
	};

	public static async Task<int> Main(string[] args)
	{
		using var client = CreateHttpClient();
		return await RunAsync(args, Environment.GetEnvironmentVariable, client,
			Environment.CurrentDirectory, Console.Out, Console.Error);
	}

	internal static async Task<int> RunAsync(IReadOnlyList<string> arguments, Func<string, string?> getEnvironmentVariable,
		HttpClient httpClient, string workingDirectory, TextWriter standardOutput, TextWriter standardError,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var options = MonitorOptions.Parse(arguments, workingDirectory);
			var manifest = WatchManifestFile.Read(workingDirectory);
			var frameworks = options.TargetFramework is null
				? manifest.Frameworks
				: manifest.Frameworks.Where(framework => framework.TargetFramework == options.TargetFramework).ToArray();
			if (!frameworks.Any())
			{
				throw new MonitorFailure("The requested target framework is not configured for upstream monitoring.");
			}
			var results = new List<(FrameworkBaseline Framework, MonitorResult Result)>();
			foreach (var framework in frameworks)
			{
				results.Add((framework, await RunMonitorAsync(new MonitorRequest(manifest, framework, options.Tag, options.Baseline),
					getEnvironmentVariable("GH_TOKEN"), httpClient, cancellationToken)));
			}
			await WriteReportAsync(options.JsonPath, CombinedJson(results), cancellationToken);
			await WriteReportAsync(options.MarkdownPath, CombinedMarkdown(results), cancellationToken);
			var status = results.Max(result => result.Result.Status);
			if (status == MonitorStatus.InfrastructureError)
			{
				await standardError.WriteLineAsync(results.First(result => result.Result.InfrastructureError is not null).Result.InfrastructureError);
			}
			else
			{
				await standardOutput.WriteLineAsync(status.ToString());
			}
			return (int)status;
		}
		catch (Exception exception)
		{
			await standardError.WriteLineAsync(MonitorErrors.SafeMessage(exception));
			return 2;
		}
	}

	private static async Task<MonitorResult> RunMonitorAsync(MonitorRequest request, string? token,
		HttpClient client, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return MonitorReports.Create(request, MonitorStatus.InfrastructureError, null, [], [], "GH_TOKEN environment variable is required.");
		}
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		client.DefaultRequestHeaders.UserAgent.ParseAdd("Htmxor-UpstreamMonitor/1.0");
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		var result = await new UpstreamMonitorApplication(client).RunAsync(request, cancellationToken);
		var issueWrite = await new GitHubIssueUpserter(client).UpsertAsync(result, cancellationToken);
		return issueWrite.Error is null ? result : MonitorReports.Create(request, MonitorStatus.InfrastructureError,
			result.Upstream, result.SourceChanges, result.ApiChanges, issueWrite.Error);
	}

	private static async Task WriteReportAsync(string path, string report, CancellationToken cancellationToken)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		await File.WriteAllTextAsync(path, report, cancellationToken);
	}

	private static string CombinedJson(IReadOnlyList<(FrameworkBaseline Framework, MonitorResult Result)> results) => results.Count == 1
		? results[0].Result.JsonReport
		: new JsonObject { ["frameworks"] = new JsonArray(results.Select(result => new JsonObject
			{ ["targetFramework"] = result.Framework.TargetFramework, ["report"] = JsonNode.Parse(result.Result.JsonReport) }).ToArray()) }
			.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

	private static string CombinedMarkdown(IReadOnlyList<(FrameworkBaseline Framework, MonitorResult Result)> results) => results.Count == 1
		? results[0].Result.MarkdownReport
		: string.Join("\n\n", results.Select(result => $"# {result.Framework.TargetFramework}\n\n{result.Result.MarkdownReport}"));
}

internal sealed record MonitorOptions(string? TargetFramework, string? Tag, string? Baseline, string JsonPath, string MarkdownPath)
{
	public static MonitorOptions Parse(IReadOnlyList<string> arguments, string root)
	{
		if (arguments.Contains("--token", StringComparer.Ordinal))
		{
			throw new MonitorFailure("Tokens are accepted only through the GH_TOKEN environment variable.");
		}
		var values = new Dictionary<string, string>(StringComparer.Ordinal);
		for (var index = 0; index < arguments.Count; index += 2)
		{
			var option = arguments[index];
			if (option is not ("--framework" or "--tag" or "--baseline" or "--json" or "--markdown") || index + 1 >= arguments.Count)
			{
				throw new MonitorFailure("Usage: [--framework TFM] [--tag TAG --baseline COMMIT] [--json PATH] [--markdown PATH].");
			}
			if (!values.TryAdd(option, arguments[index + 1]))
			{
				throw new MonitorFailure($"Option '{option}' may only be specified once.");
			}
		}
		return new(values.GetValueOrDefault("--framework"), values.GetValueOrDefault("--tag"), values.GetValueOrDefault("--baseline"),
			Path.GetFullPath(values.GetValueOrDefault("--json", "upstream-monitor.json"), root),
			Path.GetFullPath(values.GetValueOrDefault("--markdown", "upstream-monitor.md"), root));
	}
}
