namespace Htmxor.UpstreamMonitor;

internal static class Program
{
	internal static Func<HttpClient> CreateHttpClient { get; set; } = () => new HttpClient
	{
		BaseAddress = new Uri("https://api.github.com"),
	};

	public static async Task<int> Main(string[] args)
	{
		using var client = CreateHttpClient();
		return await RunAsync(args, Environment.GetEnvironmentVariable, client,
			Environment.CurrentDirectory, Console.Out, Console.Error);
	}

	internal static Task<int> RunAsync(
		IReadOnlyList<string> arguments,
		Func<string, string?> getEnvironmentVariable,
		HttpClient httpClient,
		string workingDirectory,
		TextWriter standardOutput,
		TextWriter standardError,
		CancellationToken cancellationToken = default)
	{
		_ = arguments;
		_ = getEnvironmentVariable;
		_ = httpClient;
		_ = workingDirectory;
		_ = standardOutput;
		_ = standardError;
		_ = cancellationToken;
		return Task.FromResult(2);
	}
}
