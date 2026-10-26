using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Htmxor.Samples.Tests;

internal sealed class SampleProcessLease(bool blockStartup) : IDisposable
{
	private bool disposed;

	public string MarkerPath { get; } = Path.Combine(Path.GetTempPath(), $"htmxor-readiness-{Guid.NewGuid():N}");

	public Dictionary<string, string> Environment => new()
	{
		["DOTNET_STARTUP_HOOKS"] = typeof(SampleProcessLease).Assembly.Location,
		["HTMXOR_STARTUP_PID_MARKER"] = MarkerPath,
		["HTMXOR_BLOCK_STARTUP"] = blockStartup.ToString(CultureInfo.InvariantCulture),
	};

	public Process OpenOwnedProcess()
	{
		var identity = File.ReadAllText(MarkerPath).Split('|');
		var process = Process.GetProcessById(int.Parse(identity[0], CultureInfo.InvariantCulture));
		try
		{
			Assert.Equal(identity[1], ReadStartIdentity(process));
			return process;
		}
		catch
		{
			process.Dispose();
			throw;
		}
	}

	internal static string ReadStartIdentity(Process process)
	{
		if (!OperatingSystem.IsLinux())
		{
			return process.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
		}

		// Linux StartTime converts boot time through a varying wall-clock estimate; kernel start ticks are exact.
		var stat = File.ReadAllText($"/proc/{process.Id}/stat");
		return stat[(stat.LastIndexOf(')') + 2)..].Split(' ', StringSplitOptions.RemoveEmptyEntries)[19];
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		disposed = true;
		try
		{
			StopOwnedChild();
		}
		finally
		{
			File.Delete(MarkerPath);
		}
	}

	private void StopOwnedChild()
	{
		if (!File.Exists(MarkerPath))
		{
			return;
		}

		try
		{
			using var process = OpenOwnedProcess();
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}

			Assert.True(process.WaitForExit(5000), "Owned sample process did not exit after independent cleanup.");
		}
		catch (ArgumentException)
		{
			// The host may have already reaped this exact child before independent cleanup.
		}
	}
}
