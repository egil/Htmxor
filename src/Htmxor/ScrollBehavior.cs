// ReSharper disable InconsistentNaming
namespace Htmxor;

/// <summary>
/// The behavior for a boosted link on page transitions.
/// </summary>
/// <remarks>
/// The casing on each of these values matches htmx attributes so that they can be used directly in markup
/// </remarks>
public enum ScrollBehavior
{
	/// <summary>
	/// Smooth will smooth-scroll to the top of the page.
	/// </summary>
	smooth,

	/// <summary>
	/// Auto will behave like a vanilla link.
	/// </summary>
	auto,
}

