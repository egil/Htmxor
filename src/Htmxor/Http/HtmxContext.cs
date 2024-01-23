using Htmx;
using Microsoft.AspNetCore.Http;

namespace Htmxor.Http;

/// <summary>
/// Encapsulates all HTMX-specific information related to an individual HTTP request
/// </summary>
public class HtmxContext : HtmxRequestHeaders
{
	/// <summary>
	/// Gets the <see cref="Http.HttpContext"/> for the executing action.
	/// </summary>
	public HttpContext HttpContext { get; }

	/// <summary>
	/// Gets whether the current request is an Htmx request
	/// </summary>
	public bool IsHtmx => Request.IsHtmx();

	/// <summary>
	/// Gets the <see cref="HttpRequest"/> for the executing action.
	/// </summary>
	public HttpRequest Request => HttpContext?.Request!;

	/// <summary>
	/// Gets the <see cref="HttpResponse"/> for the executing action.
	/// </summary>
	public HttpResponse Response => HttpContext?.Response!;

	/// <summary>
	/// Invokes an htmx response method
	/// </summary>
	/// <code>
	/// context.InvokeHtmx(h => {
	///		h.PushUrl("/new-url")
	///		 .WithTrigger("cool")
	/// }
	/// </code>
	/// <param name="action"></param>
	public void InvokeHtmx(Action<HtmxResponseHeaders> action) => Response.Htmx(action);

	public HtmxContext(HttpContext context) : base(context.Request)
    {
        HttpContext = context;
	}
}
