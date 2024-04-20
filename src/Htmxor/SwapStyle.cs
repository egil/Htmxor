namespace Htmxor;

/// <summary>
/// How to swap the response into the target element.
/// </summary>
public enum SwapStyle
{
    /// <summary>
    /// Replace the inner html of the target element.
    /// </summary>
    InnerHTML,

    /// <summary>
    /// Replace the entire target element with the response.
    /// </summary>
    OuterHTML,

    /// <summary>
    /// Replace the text content of the target element, without parsing the response as HTML.
    /// </summary>
    /// <remarks>
    /// NOTE: Available in Htmx version 2 and later.
    /// </remarks>
    // TextContent,

    /// <summary>
    /// Insert the response before the target element.
    /// </summary>
    BeforeBegin,

    /// <summary>
    /// Insert the response before the first child of the target element.
    /// </summary>
    AfterBegin,

    /// <summary>
    /// Insert the response after the last child of the target element.
    /// </summary>
    BeforeEnd,

    /// <summary>
    /// Insert the response after the target element.
    /// </summary>
    AfterEnd,

    /// <summary>
    /// Deletes the target element regardless of the response.
    /// </summary>
    Delete,

    /// <summary>
    /// Does not append content from response (out of band items will still be processed).
    /// </summary>
    None,
}

