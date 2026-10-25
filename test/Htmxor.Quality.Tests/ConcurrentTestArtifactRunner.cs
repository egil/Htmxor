using System.Collections.Concurrent;
using System.Xml.Linq;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed class ConcurrentTestArtifactRunner(bool failing) : IProcessRunner
{
	public const string Head = "0123456789abcdef0123456789abcdef01234567";
	private readonly TaskCompletionSource allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public ConcurrentQueue<ProcessCommand> Commands { get; } = new();

	public IReadOnlyList<Boundary> Boundaries { get; } = CreateBoundaries(failing);

	public async Task<int> ObserveOverlapAsync()
	{
		var observed = 0;
		try
		{
			// Time bounds failure only. No child can finish until every dispatch has been observed.
			await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
		}
		catch (TimeoutException)
		{
		}
		finally
		{
			observed = Commands.Count;
			release.TrySetResult();
		}

		return observed;
	}

	public async Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
	{
		if (command.FileName == "git")
		{
			return new(0, command.Arguments[0] == "rev-parse" ? Head : string.Empty, string.Empty);
		}

		if (command.Arguments[0] != "test")
		{
			return new(0, string.Empty, string.Empty);
		}

		Commands.Enqueue(command);
		if (Commands.Count == 6)
		{
			allStarted.TrySetResult();
		}

		await release.Task.WaitAsync(cancellationToken);
		return WriteEvidence(command);
	}

	private ProcessResult WriteEvidence(ProcessCommand command)
	{
		var directory = ProcessCommandArguments.After(command.Arguments, "--results-directory");
		var boundary = Boundaries.Single(boundary => boundary.Name == Path.GetFileName(directory));
		var counts = boundary.Counts;
		var counters = new XElement("Counters",
			new XAttribute("total", counts.Total), new XAttribute("executed", counts.Executed),
			new XAttribute("passed", counts.Passed), new XAttribute("failed", counts.Failed),
			new XAttribute("notExecuted", counts.Skipped), new XAttribute("error", counts.Errors),
			new XAttribute("timeout", counts.TimedOut));
		new XElement("TestRun", new XElement("ResultSummary", counters))
			.Save(Path.Combine(directory, $"{boundary.Name}.trx"));
		if (command.Arguments.Contains("--collect", StringComparer.Ordinal))
		{
			WriteCoverageCopies(directory);
		}

		return new(boundary.ExitCode, string.Empty, string.Empty);
	}

	private static void WriteCoverageCopies(string directory)
	{
		foreach (var name in new[] { "coverage", "deployment" })
		{
			var destination = Path.Combine(directory, name);
			Directory.CreateDirectory(destination);
			File.WriteAllText(Path.Combine(destination, "coverage.cobertura.xml"), "<coverage line-rate=\"1\" />");
		}
	}

	private static Boundary[] CreateBoundaries(bool failing) =>
		[
			new("upstream-fixtures", "test/Htmxor.UpstreamMonitor.Tests/Htmxor.UpstreamMonitor.Tests.csproj",
				failing ? new(0, 0, 0, 0, 0, 0, 0) : new(3, 2, 2, 0, 1, 0, 0), 0),
			new("quality", "test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj", new(4, 3, 3, 0, 1, 0, 0), 0),
			new("aspnetcore10", "test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj", new(5, 4, 4, 0, 1, 0, 0), 0),
			new("aspnetcore11", "test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj", new(6, 5, 5, 0, 1, 0, 0), 0),
			new("samples", "test/Htmxor.Samples.Tests/Htmxor.Samples.Tests.csproj",
				failing ? new(17, 15, 11, 1, 2, 1, 2) : new(7, 6, 6, 0, 1, 0, 0), failing ? 7 : 0),
			new("htmxor", "test/Htmxor.Tests/Htmxor.Tests.csproj", new(8, 7, 7, 0, 1, 0, 0), 0),
		];

	internal sealed record Boundary(string Name, string Project, TrxTestRun Counts, int ExitCode);
}
