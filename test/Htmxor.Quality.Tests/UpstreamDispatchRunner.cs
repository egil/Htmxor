using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

internal sealed class UpstreamDispatchRunner(int monitorExit, string? failurePhase = null) : IProcessRunner
{
	public List<string> Phases { get; } = [];

	public List<ProcessCommand> Commands { get; } = [];

	public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
	{
		if (command.FileName == "git")
		{
			return Task.FromResult(new ProcessResult(0, command.Arguments.Contains("rev-parse", StringComparer.Ordinal)
				? "0123456789abcdef0123456789abcdef01234567\n" : string.Empty, string.Empty));
		}

		Commands.Add(command);
		var phase = Phase(command);
		Phases.Add(phase);
		return Task.FromResult(Execute(command, phase));
	}

	private ProcessResult Execute(ProcessCommand command, string phase)
	{
		if (phase == "monitor")
		{
			return new ProcessResult(monitorExit, string.Empty, string.Empty);
		}

		var failed = phase == failurePhase;
		WriteTestEvidence(command, failed);

		if (failed && command.EnsureSuccess)
		{
			throw new InvalidOperationException($"{phase} failed.");
		}

		return new ProcessResult(failed ? 1 : 0, string.Empty, string.Empty);
	}

	private static void WriteTestEvidence(ProcessCommand command, bool failed)
	{
		if (command.Arguments.Contains("--logger", StringComparer.Ordinal))
		{
			WriteTrx(command, failed);
		}
	}

	private static string Phase(ProcessCommand command)
	{
		var first = command.Arguments[0];
		return first switch
		{
			"format" => command.Arguments[1],
			"test" => command.Arguments[1].Contains("Htmxor.Quality.Tests", StringComparison.Ordinal) ? "policy" : "fixtures",
			"run" => "monitor",
			_ => first,
		};
	}

	private static void WriteTrx(ProcessCommand command, bool failed)
	{
		var args = command.Arguments.ToList();
		var directory = args[args.IndexOf("--results-directory") + 1];
		var logger = args[args.IndexOf("--logger") + 1];
		var filename = logger[(logger.IndexOf('=') + 1)..];
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, filename),
			$"<TestRun><ResultSummary><Counters total=\"1\" executed=\"1\" passed=\"{(failed ? 0 : 1)}\" failed=\"{(failed ? 1 : 0)}\" notExecuted=\"0\" error=\"0\" timeout=\"0\" /></ResultSummary></TestRun>");
	}
}
