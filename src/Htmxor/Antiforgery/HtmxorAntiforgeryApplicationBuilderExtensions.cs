using Htmxor.Antiforgery;

// Disabled since this is an file containing extension method's which
// should share a namespace with the type they target for easy discoverability.
#pragma warning disable IDE0130
namespace Microsoft.AspNetCore.Builder;
#pragma warning restore IDE0130

/// <summary>
/// This class has extension methods <see cref="IApplicationBuilder"/> 
/// that enable configuration of Antiforgery logic for Htmx in the application.
/// </summary>
public static class HtmxorAntiforgeryApplicationBuilderExtensions
{
	/// <summary>
	/// Enable Htmx to use antiforgery tokens to secure requests.
	/// </summary>
	/// <param name="builder"></param>
	/// <returns></returns>
	public static IApplicationBuilder UseHtmxAntiforgery(this IApplicationBuilder builder)
	{
		builder.UseMiddleware<HtmxorAntiforgeryMiddleware>();
		return builder;
	}
}
