using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Endpoints;

public interface IRazorComponentEndpointInvoker
{
	ValueTask InvokeAsync(HttpContext context);

	Task WarmAsync(CancellationToken cancellationToken);
}

internal static class UpstreamSourceExecutionSentinel
{
	[ModuleInitializer]
	internal static void MarkExecuted() => File.WriteAllText("__SENTINEL_PATH__", "executed");
}
