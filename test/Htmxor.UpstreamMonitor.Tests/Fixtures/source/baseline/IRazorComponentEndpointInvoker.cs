namespace Microsoft.AspNetCore.Components.Endpoints;

public interface IRazorComponentEndpointInvoker
{
	Task InvokeAsync(HttpContext context);
}
