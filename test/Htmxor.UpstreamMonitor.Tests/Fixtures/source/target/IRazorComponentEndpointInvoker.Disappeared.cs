namespace Microsoft.AspNetCore.Components.Endpoints;

public interface IReplacementEndpointInvoker
{
	Task InvokeAsync(HttpContext context);
}
