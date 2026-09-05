using System.Net.Http.Headers;

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
			var request = new MonitorRequest(WatchManifestFile.Read(workingDirectory), 10, options.Tag, options.Baseline);
			var result = await RunMonitorAsync(request, getEnvironmentVariable("GH_TOKEN"), httpClient, cancellationToken);
			await WriteReportAsync(options.JsonPath, result.JsonReport, cancellationToken);
			await WriteReportAsync(options.MarkdownPath, result.MarkdownReport, cancellationToken);
			if (result.InfrastructureError is not null)
			{
				await standardError.WriteLineAsync(result.InfrastructureError);
			}
			else
			{
				await standardOutput.WriteLineAsync(result.Status.ToString());
			}
			return (int)result.Status;
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
}

internal sealed record MonitorOptions(string? Tag, string? Baseline, string JsonPath, string MarkdownPath)
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
			if (arguments[index] is not ("--tag" or "--baseline" or "--json" or "--markdown") || index + 1 >= arguments.Count)
			{
				throw new MonitorFailure("Usage: [--tag TAG --baseline COMMIT] [--json PATH] [--markdown PATH].");
			}
			values.Add(arguments[index], arguments[index + 1]);
		}
		return new(values.GetValueOrDefault("--tag"), values.GetValueOrDefault("--baseline"),
			Path.GetFullPath(values.GetValueOrDefault("--json", "upstream-monitor.json"), root),
			Path.GetFullPath(values.GetValueOrDefault("--markdown", "upstream-monitor.md"), root));
	}
}
