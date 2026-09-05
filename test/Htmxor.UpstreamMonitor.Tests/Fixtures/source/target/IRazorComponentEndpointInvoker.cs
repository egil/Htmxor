namespace Microsoft.AspNetCore.Components.Endpoints;

public interface IRazorComponentEndpointInvoker
{
	ValueTask InvokeAsync(HttpContext context);

	Task WarmAsync(CancellationToken cancellationToken);
}
