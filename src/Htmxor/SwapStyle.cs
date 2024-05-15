// ReSharper disable InconsistentNaming
namespace Htmxor;

/// <summary>
/// How to swap the response into the target element.
/// </summary>
public enum SwapStyle
{
	/// <summary>
	/// Default style is what is specified in <see cref="HtmxConfig.DefaultSwapStyle"/> for the application
	/// or htmx's default, which is <see cref="innerHTML"/>.
	/// </summary>
	Default,

	/// <summary>
	/// Replace the inner html of the target element.
	/// </summary>
	innerHTML,

	/// <summary>
	/// Replace the entire target element with the response.
	/// </summary>
	outerHTML,

	/// <summary>
	/// Insert the response before the target element.
	/// </summary>
	beforebegin,

	/// <summary>
	/// Insert the response before the first child of the target element.
	/// </summary>
	afterbegin,

	/// <summary>
	/// Insert the response after the last child of the target element.
	/// </summary>
	beforeend,

	/// <summary>
	/// Insert the response after the target element.
	/// </summary>
	afterend,

	/// <summary>
	/// Deletes the target element regardless of the response.
	/// </summary>
	delete,

	/// <summary>
	/// Does not append content from response (out of band items will still be processed).
	/// </summary>
	none,
}

