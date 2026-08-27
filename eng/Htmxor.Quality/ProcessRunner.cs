using System.Diagnostics;
using System.Text;

namespace Htmxor.Quality;

internal sealed class ProcessRunner : IProcessRunner
{
	public async Task<ProcessResult> RunAsync(
		ProcessCommand command,
		CancellationToken cancellationToken = default)
	{
		Console.WriteLine($"> {command.Display}");
		var startInfo = CreateStartInfo(command);
		using var process = new Process { StartInfo = startInfo };
		process.Start();

		var outputTask = PumpAsync(process.StandardOutput, Console.Out);
		var errorTask = PumpAsync(process.StandardError, Console.Error);
		await process.WaitForExitAsync(cancellationToken);
		var output = await outputTask;
		var error = await errorTask;
		var result = new ProcessResult(process.ExitCode, output, error);

		if (command.EnsureSuccess && result.ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"{command.FileName} exited with code {result.ExitCode}.");
		}

		return result;
	}

	private static ProcessStartInfo CreateStartInfo(ProcessCommand command)
	{
		var startInfo = new ProcessStartInfo(command.FileName)
		{
			WorkingDirectory = command.WorkingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
		startInfo.Environment["DOTNET_NOLOGO"] = "1";
		foreach (var argument in command.Arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		return startInfo;
	}

	private static async Task<string> PumpAsync(StreamReader reader, TextWriter writer)
	{
		var output = new StringBuilder();
		while (await reader.ReadLineAsync() is { } line)
		{
			output.AppendLine(line);
			await writer.WriteLineAsync(line);
		}

		return output.ToString();
	}
}
