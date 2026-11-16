using Htmxor.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Htmxor extensions on <see cref="OutputCachePolicyBuilder"/>.
/// </summary>
public static class HtmxorOutputCachePolicyBuilderExtensions
{
	/// <summary>
	/// Varies the policy's cache entries by the <c>HX-Request</c> and <c>HX-Request-Type</c> request headers,
	/// so a URL's full page and its direct htmx representation are cached and served separately.
	/// Every variation the policy already declares still applies.
	/// </summary>
	/// <param name="builder">The output-cache policy builder.</param>
	/// <returns>The <paramref name="builder"/>, for chaining.</returns>
	public static OutputCachePolicyBuilder VaryByHtmx(this OutputCachePolicyBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		return builder.SetVaryByHeader([.. HtmxRepresentationVary.RepresentationHeaders]);
	}
}
