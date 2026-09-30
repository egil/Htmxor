namespace Htmxor.AspNetCore10;

// One stock and one candidate switch-on host process, shared by every Issue264SwitchOnTests case. xUnit runs
// the test methods of a single class sequentially by default, so sharing these long-lived processes keeps
// per-test process-startup cost down without any of them observing another's in-flight request.
public sealed class Issue264SwitchOnFixture : IAsyncLifetime
{
	public Issue264SwitchOnHostProcess Stock { get; private set; } = null!;

	public Issue264SwitchOnHostProcess Candidate { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Stock = await Issue264SwitchOnHostProcess.StartAsync("stock");
		Candidate = await Issue264SwitchOnHostProcess.StartAsync("candidate");
	}

	public async Task DisposeAsync()
	{
		await Stock.DisposeAsync();
		await Candidate.DisposeAsync();
	}
}
