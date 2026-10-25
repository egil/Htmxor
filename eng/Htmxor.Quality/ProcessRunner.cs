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

		var label = OutputLabel(command);
		var outputTask = PumpAsync(process.StandardOutput, Console.Out, label);
		var errorTask = PumpAsync(process.StandardError, Console.Error, label);
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
		if (command.FileName == "dotnet")
		{
			// VSTest inherits SDK paths from its build. Child workspaces select their own SDK through global.json.
			startInfo.Environment.Remove("MSBuildSDKsPath");
			startInfo.Environment.Remove("MSBUILD_EXE_PATH");
			startInfo.Environment.Remove("MSBuildExtensionsPath");
			// Reusable MSBuild nodes can outlive the command while retaining its redirected output pipes.
			startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
		}
		foreach (var argument in command.Arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		return startInfo;
	}

	private static string? OutputLabel(ProcessCommand command)
	{
		if (command.FileName != "dotnet" || command.Arguments.Count < 2 || command.Arguments[0] != "test")
		{
			return null;
		}

		var label = Path.GetFileName(command.Arguments[1]);
		var framework = command.Arguments.SkipWhile(argument => argument != "--framework").Skip(1).FirstOrDefault();
		return framework is null ? label : $"{label}/{framework}";
	}

	private static async Task<string> PumpAsync(StreamReader reader, TextWriter writer, string? label)
	{
		var output = new StringBuilder();
		while (await reader.ReadLineAsync() is { } line)
		{
			output.AppendLine(line);
			await writer.WriteLineAsync(label is null ? line : $"[{label}] {line}");
		}

		return output.ToString();
	}
}
