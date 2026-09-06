using Htmxor.UpstreamMonitor;

namespace Htmxor.UpstreamMonitor.Tests;

[CollectionDefinition("Process environment", DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection;

internal sealed class MonitorProcessScope : IDisposable
{
	private readonly string directory = Environment.CurrentDirectory;
	private readonly string? token = Environment.GetEnvironmentVariable("GH_TOKEN");
	private readonly TextWriter output = Console.Out;
	private readonly TextWriter error = Console.Error;
	private readonly Func<HttpClient> clientFactory = Program.CreateHttpClient;

	public MonitorProcessScope(string workingDirectory, string? environmentToken, HttpClient client,
		TextWriter standardOutput, TextWriter standardError)
	{
		Environment.CurrentDirectory = workingDirectory;
		Environment.SetEnvironmentVariable("GH_TOKEN", environmentToken);
		Console.SetOut(standardOutput);
		Console.SetError(standardError);
		Program.CreateHttpClient = () => client;
	}

	public void Dispose()
	{
		Program.CreateHttpClient = clientFactory;
		Console.SetError(error);
		Console.SetOut(output);
		Environment.SetEnvironmentVariable("GH_TOKEN", token);
		Environment.CurrentDirectory = directory;
	}
}
