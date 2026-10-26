// The runtime locates startup hooks by this exact, unqualified type name.
internal static class StartupHook
{
	public static void Initialize()
	{
		var marker = Environment.GetEnvironmentVariable("HTMXOR_STARTUP_PID_MARKER");
		if (marker is null)
		{
			return;
		}

		using var process = System.Diagnostics.Process.GetCurrentProcess();
		File.WriteAllText(marker + ".tmp", FormattableString.Invariant($"{process.Id}|{Htmxor.Samples.Tests.SampleProcessLease.ReadStartIdentity(process)}"));
		File.Move(marker + ".tmp", marker);
		var lifetime = new Thread(() =>
		{
			Thread.Sleep(TimeSpan.FromSeconds(75));
			Environment.Exit(124);
		}) { IsBackground = true, Name = "Readiness fixture child lifetime" };
		lifetime.Start();
		if (Environment.GetEnvironmentVariable("HTMXOR_BLOCK_STARTUP") == "True")
		{
			Console.WriteLine("Sample startup blocked before application entry point.");
			Console.Error.WriteLine("Sample startup stderr marker.");
			Thread.Sleep(Timeout.Infinite);
		}
	}
}
