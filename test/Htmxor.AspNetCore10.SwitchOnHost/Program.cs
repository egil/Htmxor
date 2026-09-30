using Htmxor;
using Htmxor.AspNetCore10.SwitchOnHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

if (!Issue264SwitchOnHostRuntime.TryParseMode(args, out var mode))
{
	Console.Error.WriteLine("Usage: Htmxor.AspNetCore10.SwitchOnHost <stock|candidate|candidate-switch-off>");
	return 1;
}

// The parent test process (Issue264SwitchOnHostProcess) waits for this line and refuses to trust any
// candidate-vs-oracle comparison unless it matches this mode's expected value: a switch that never reached
// this process must fail loudly here, not appear as silent candidate parity later.
var observedSwitch = Issue264SwitchOnHostRuntime.ConfigureAndObserveSwitch(mode);
Console.WriteLine($"SWITCH {observedSwitch}");
if (observedSwitch != Issue264SwitchOnHostRuntime.ExpectedSwitch(mode))
{
	Console.Error.WriteLine(
		$"Refusing to start: mode '{mode}' expected the navigation switch to be " +
		$"{Issue264SwitchOnHostRuntime.ExpectedSwitch(mode)} but observed {observedSwitch}.");
	return 2;
}

Issue264SwitchOnHostRuntime.ReportUnobservedNavigationExceptions();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = "Issue264SwitchOnHost" });
Issue264SwitchOnHostRuntime.ConfigureServices(builder, mode);

var app = builder.Build();
app.UseAntiforgery();
Issue264SwitchOnHostRuntime.ConfigureGcForcingMiddleware(app);
Issue264SwitchOnHostRuntime.ConfigureEndpoints(app, mode);

await app.StartAsync();
Console.WriteLine($"LISTENING {app.Urls.First()}");
Issue264SwitchOnHostRuntime.StopWhenStandardInputCloses(app);
await app.WaitForShutdownAsync();
return 0;
