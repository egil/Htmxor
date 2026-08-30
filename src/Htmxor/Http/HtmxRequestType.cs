namespace Htmxor.Http;

/// <summary>
/// Identifies whether htmx requested a full-page or partial representation.
/// </summary>
public enum HtmxRequestType
{
	/// <summary>
	/// The request targets the whole page or selects content from a full-page response.
	/// </summary>
	Full,

	/// <summary>
	/// The request targets a specific element and can use the direct component representation.
	/// </summary>
	Partial,
}
