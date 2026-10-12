namespace Htmxor;

/// <summary>
/// Declares a supported HTTP action on the route-owning component's request-instance callback.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class HtmxActionAttribute(string method) : Attribute
{
	/// <summary>
	/// Gets the declared HTTP method: POST, PUT, PATCH, DELETE, or QUERY, case-insensitively.
	/// </summary>
	public string Method { get; } = method;
}
