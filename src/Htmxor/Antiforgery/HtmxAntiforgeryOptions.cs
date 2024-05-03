using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;

namespace Htmxor.Antiforgery;

/// <summary>
/// Represents the options for Htmxor's antiforgery support.
/// </summary>
internal class HtmxAntiforgeryOptions(IOptions<AntiforgeryOptions> antiforgeryOptions)
{
	/// <summary>
	/// Gets the name of the form field used for antiforgery token.
	/// </summary>
	public string FormFieldName { get; } = antiforgeryOptions.Value.FormFieldName;

	/// <summary>
	/// Gets the name of the header used for antiforgery token.
	/// </summary>
	public string? HeaderName { get; } = antiforgeryOptions.Value.HeaderName;

	/// <summary>
	/// Gets the name of the cookie used for antiforgery token.
	/// </summary>
	public string CookieName { get; } = "HX-XSRF-TOKEN";
}
