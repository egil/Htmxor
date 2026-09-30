using Htmxor;
using Htmxor.AspNetCore10.SwitchOnHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var mode = args.Length > 0 ? args[0] : null;
if (mode is not "stock" and not "candidate")
{
	Console.Error.WriteLine("Usage: Htmxor.AspNetCore10.SwitchOnHost <stock|candidate>");
	return 1;
}

// The parent test process (Issue264SwitchOnTests, in Htmxor.AspNetCore10.Tests) treats this line, written to
// this process's own stdout, as its "no unobserved navigation exception" evidence: the buggy candidate spawns
// a fire-and-forget task from HttpNavigationManager.NavigateToCore that throws InvalidOperationException when
// the endpoint-based navigation callback was never supplied.
TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
{
	var exception = eventArgs.Exception.GetBaseException();
	Console.WriteLine($"UNOBSERVED {exception.GetType().FullName}: {exception.Message}");
	eventArgs.SetObserved();
};

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = "Issue264SwitchOnHost" });
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
var razorComponents = builder.Services.AddRazorComponents();
if (mode == "candidate")
{
	razorComponents.AddHtmxor();
}

var app = builder.Build();
app.UseAntiforgery();

// A fire-and-forget faulted task settles synchronously with the request that spawned it (see
// HttpNavigationManager.NavigateToCore), but stays merely unreferenced, not unobserved, until it is
// collected. Forcing two full blocking collections plus a finalizer drain after every response makes the
// runtime raise TaskScheduler.UnobservedTaskException deterministically within the same request/response the
// test is asserting on, instead of leaving it to an arbitrary later GC.
app.Use(async (context, next) =>
{
	await next(context);
	GC.Collect(2, GCCollectionMode.Forced, blocking: true);
	GC.WaitForPendingFinalizers();
	GC.Collect(2, GCCollectionMode.Forced, blocking: true);
});

var endpoints = app.MapRazorComponents<Issue264SwitchOnApp>();
if (mode == "candidate")
{
	endpoints.AddHtmxorEndpoints();
}

await app.StartAsync();
Console.WriteLine($"LISTENING {app.Urls.First()}");
await app.WaitForShutdownAsync();
return 0;
