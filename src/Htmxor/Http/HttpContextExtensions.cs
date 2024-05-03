using Htmxor.Http;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Htmxor extensions on <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
	private const string HtmxContextKey = "HtmxContext";

	/// <summary>
	/// Gets the <see cref="HtmxContext"/> associated with the current request.
	/// </summary>
	/// <param name="httpContext">The <see cref="HttpContext"/> to get the <see cref="HtmxContext"/> from.</param>
	public static HtmxContext GetHtmxContext(this HttpContext httpContext)
	{
		ArgumentNullException.ThrowIfNull(httpContext);

		if (!httpContext.Items.TryGetValue(HtmxContextKey, out var value) || value is not HtmxContext result)
		{
			result = new HtmxContext(httpContext);
			httpContext.Items[HtmxContextKey] = result;
		}

		return result;
	}
}
