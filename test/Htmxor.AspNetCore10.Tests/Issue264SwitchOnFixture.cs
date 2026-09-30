namespace Htmxor.AspNetCore10;

// One stock, one candidate, and one candidate-switch-off host process, shared by every Issue264SwitchOnTests
// case. xUnit runs the test methods of a single class sequentially by default, so sharing these long-lived
// processes keeps per-test process-startup cost down without any of them observing another's in-flight
// request. "candidate-switch-off" is the paired oracle the owner's htmx decision on #264 requires
// (https://github.com/egil/Htmxor/issues/264#issuecomment-5901861030): htmx cases compare the switch-on
// candidate to Htmxor's own switch-off (throwing-path) candidate, never to stock.
public sealed class Issue264SwitchOnFixture : IAsyncLifetime
{
	public Issue264SwitchOnHostProcess Stock { get; private set; } = null!;

	public Issue264SwitchOnHostProcess Candidate { get; private set; } = null!;

	public Issue264SwitchOnHostProcess CandidateSwitchOff { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Stock = await Issue264SwitchOnHostProcess.StartAsync("stock");
		Candidate = await Issue264SwitchOnHostProcess.StartAsync("candidate");
		CandidateSwitchOff = await Issue264SwitchOnHostProcess.StartAsync("candidate-switch-off");

		// Each host already refused to start (see Program.cs) unless its own AppContext.TryGetSwitch reading
		// matched what its mode requires, so this is belt-and-suspenders, not the only guard: it turns a
		// process that somehow got here anyway into an immediate, attributable fixture failure instead of a
		// silent false green in every case that uses it.
		Assert.True(Stock.ObservedSwitch, "The stock switch-on host must observe the navigation switch as on.");
		Assert.True(Candidate.ObservedSwitch, "The candidate switch-on host must observe the navigation switch as on.");
		Assert.False(
			CandidateSwitchOff.ObservedSwitch,
			"The candidate switch-off host must observe the navigation switch as off.");
	}

	public async Task DisposeAsync()
	{
		await Stock.DisposeAsync();
		await Candidate.DisposeAsync();
		await CandidateSwitchOff.DisposeAsync();
	}
}
