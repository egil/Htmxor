namespace Htmxor;

/// <summary>
/// Enumerates the HTTP methods supported by Htmx.
/// </summary>
[Flags]
public enum HttpMethod
{
    /// <summary>
    /// Route supports all HTTP methods supported by Htmx.
    /// </summary>
    All = 0,
    /// <summary>
    /// Route supports GET requests.
    /// </summary>
    Get = 1,
    /// <summary>
    /// Route supports POST requests.
    /// </summary>
    Post = 2,
    /// <summary>
    /// Route supports PUT requests.
    /// </summary>
    Put = 4,
    /// <summary>
    /// Route supports PATCH requests.
    /// </summary>
    Patch = 8,
    /// <summary>
    /// Route supports DELETE requests.
    /// </summary>
    Delete = 16,
}