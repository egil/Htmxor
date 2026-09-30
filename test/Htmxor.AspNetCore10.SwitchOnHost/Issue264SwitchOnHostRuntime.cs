using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10.SwitchOnHost;

// Keeps Program.cs's top-level statements a short, low-complexity sequence by holding every per-mode decision
// here instead. "stock" and "candidate" both run with the switch on; "candidate-switch-off" is the paired
// oracle the owner's htmx decision on #264 requires (see
// https://github.com/egil/Htmxor/issues/264#issuecomment-5901861030): htmx's switch-on candidate output must
// equal Htmxor's own switch-off (throwing-path) output for the same request, not stock's.
internal static class Issue264SwitchOnHostRuntime
{
	private const string SwitchName =
		"Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException";

	public static bool TryParseMode(string[] args, out string mode)
	{
		mode = args.Length > 0 ? args[0] : string.Empty;
		return mode is "stock" or "candidate" or "candidate-switch-off";
	}

	public static bool IsCandidateMode(string mode) => mode is "candidate" or "candidate-switch-off";

	public static bool ExpectedSwitch(string mode) => mode is "stock" or "candidate";

	// Reads and reports the switch exactly as HttpNavigationManager does (same AppContext switch name, same
	// AppContext.TryGetSwitch API), so "SWITCH <value>" on stdout is not a claim independent of what the
	// framework itself would observe.
	public static bool ConfigureAndObserveSwitch(string mode)
	{
		if (mode == "candidate-switch-off")
		{
			// The csproj always bakes the switch on (see its RuntimeHostConfigurationOption); this is the one
			// mode that needs it off instead, for the paired oracle the owner's htmx decision on #264 requires.
			// This runs before any request, and therefore before HttpNavigationManager's first type touch on
			// either target (net11.0 caches the switch in a `static readonly` field at that point).
			AppContext.SetSwitch(SwitchName, false);
		}

		// For "stock" and "candidate", no override happens above: reading it back here also proves the
		// csproj's RuntimeHostConfigurationOption actually reached this running process, rather than just
		// re-asserting an override this same method just made.
		return AppContext.TryGetSwitch(SwitchName, out var observed) && observed;
	}

	public static void ReportUnobservedNavigationExceptions()
	{
		// The parent test process treats a line starting with "UNOBSERVED " on this process's own stdout as
		// evidence that HttpNavigationManager.NavigateToCore's fire-and-forget PerformNavigationAsync() faulted
		// because the endpoint-based navigation callback was never supplied.
		TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
		{
			var exception = eventArgs.Exception.GetBaseException();
			Console.WriteLine($"UNOBSERVED {exception.GetType().FullName}: {exception.Message}");
			eventArgs.SetObserved();
		};
	}

	// Orthogonal to `mode`: only test/Htmxor.AspNetCore10.Tests's opaque-redirection fault-injection probe
	// (PR #265 review discussion_r4140656921) passes this, on either a "stock" or "candidate" host, to
	// isolate whether OpaqueRedirection's own protector throwing mid-write leaves a malformed <blazor-ssr>
	// fragment on the wire.
	public const string ThrowOnOpaqueRedirectionProtectFlag = "--throw-on-opaque-redirection-protect";

	public static void ConfigureServices(WebApplicationBuilder builder, string mode, bool throwOnOpaqueRedirectionProtect)
	{
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		builder.Logging.SetMinimumLevel(LogLevel.Error);
		builder.Logging.AddProvider(new Issue264ErrorLogRelay());
		if (throwOnOpaqueRedirectionProtect)
		{
			// Bypasses AddDataProtection()'s own DI wiring entirely, rather than trying to override or decorate
			// its registration afterward, so this has no dependency on that wiring's internal registration order.
			builder.Services.AddSingleton<IDataProtectionProvider>(
				new Issue264ThrowingRedirectionProtectionProvider(new EphemeralDataProtectionProvider()));
		}
		else
		{
			builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
		}

		builder.Services.AddSingleton<Issue264StreamingGate>();
		var razorComponents = builder.Services.AddRazorComponents();
		if (IsCandidateMode(mode))
		{
			razorComponents.AddHtmxor();
		}
	}

	public static void ConfigureGcForcingMiddleware(WebApplication app)
	{
		// A fire-and-forget faulted task settles synchronously with the request that spawned it (see
		// HttpNavigationManager.NavigateToCore), but stays merely unreferenced, not unobserved, until it is
		// collected. Forcing two full blocking collections plus a finalizer drain after every response makes
		// the runtime raise TaskScheduler.UnobservedTaskException deterministically within the same
		// request/response the test is asserting on, instead of leaving it to an arbitrary later GC.
		app.Use(async (context, next) =>
		{
			await next(context);
			GC.Collect(2, GCCollectionMode.Forced, blocking: true);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, blocking: true);
		});
	}

	public static void ConfigureEndpoints(WebApplication app, string mode)
	{
		var endpoints = app.MapRazorComponents<Issue264SwitchOnApp>();
		if (IsCandidateMode(mode))
		{
			endpoints.AddHtmxorEndpoints();
		}

		// Lets the test control exactly when Issue264StreamingPage navigates and when it resumes afterward
		// (see Issue264StreamingGate), instead of a fixed sleep racing the actual async completion order.
		app.MapPost("/issue-264/streaming/release-navigate", (Issue264StreamingGate gate) => gate.ReleaseNavigate());
		app.MapPost("/issue-264/streaming/release-resume", (Issue264StreamingGate gate) => gate.ReleaseResume());
	}

	// Ties this process's lifetime to its parent's: the parent test process holds this process's stdin open,
	// so EOF means the parent exited, however abruptly (a crash, a --blame-hang kill, or a debugger stop),
	// and this process should not keep listening on a leaked port and holding the pinned runtime's files open.
	public static void StopWhenStandardInputCloses(WebApplication app) =>
		_ = Task.Run(async () =>
		{
			await Console.In.ReadToEndAsync();
			app.Lifetime.StopApplication();
		});
}
