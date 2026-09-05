namespace Htmxor.UpstreamMonitor;

internal static class Program
{
	public static int Main() => 2;

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
