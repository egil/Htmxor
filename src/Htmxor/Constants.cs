namespace Htmxor;

/// <summary>
/// Useful string constants in Htmxor components.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Only containers for constants here. Nothing to see!")]
public static class Constants
{
	/// <summary>
	/// Http method names supported by htmx and Htmxor.
	/// </summary>
	public static class HttpMethods
	{
		public const string Get = "GET";
		public const string Post = "POST";
		public const string Put = "PUT";
		public const string Delete = "DELETE";
		public const string Patch = "PATCH";

		/// <summary>
		/// Returns a value that indicates if the HTTP request method is GET.
		/// </summary>
		/// <param name="method">The  HTTP request method.</param>
		/// <returns>
		/// <see langword="true" /> if the method is GET; otherwise, <see langword="false" />.
		/// </returns>
		public static bool IsGet(string method)
		{
			return MethodEquals(Get, method);
		}

		/// <summary>
		/// Returns a value that indicates if the HTTP request method is PATCH.
		/// </summary>
		/// <param name="method">The HTTP request method.</param>
		/// <returns>
		/// <see langword="true" /> if the method is PATCH; otherwise, <see langword="false" />.
		/// </returns>
		public static bool IsPatch(string method)
		{
			return MethodEquals(Patch, method);
		}

		/// <summary>
		/// Returns a value that indicates if the HTTP request method is POST.
		/// </summary>
		/// <param name="method">The HTTP request method.</param>
		/// <returns>
		/// <see langword="true" /> if the method is POST; otherwise, <see langword="false" />.
		/// </returns>
		public static bool IsPost(string method)
		{
			return MethodEquals(Post, method);
		}

		/// <summary>
		/// Returns a value that indicates if the HTTP request method is PUT.
		/// </summary>
		/// <param name="method">The HTTP request method.</param>
		/// <returns>
		/// <see langword="true" /> if the method is PUT; otherwise, <see langword="false" />.
		/// </returns>
		public static bool IsPut(string method)
		{
			return MethodEquals(Put, method);
		}

		private static bool MethodEquals(string methodA, string methodB) => StringComparer.OrdinalIgnoreCase.Equals(methodA, methodB);
	}

	/// <summary>
	/// Htmx attribute names.
	/// </summary>
	public static class Attributes
	{
		internal const string Prefix = "hx-";

		public const string Id = "id";

		public const string HxGet = "hx-get";
		public const string HxPost = "hx-post";
		public const string HxPut = "hx-put";
		public const string HxDelete = "hx-delete";
		public const string HxPatch = "hx-patch";
		public const string HxTrigger = "hx-trigger";
		public const string HxTarget = "hx-target";
		public const string HxSwap = "hx-swap";
		public const string HxHeaders = "hx-headers";

		internal const string HxorEventId = "hxor-eventid";
	}

	/// <summary>
	/// Htmx trigger values for <c>hx-trigger</c>.
	/// </summary>
	public static class Triggers
	{
		public const string Every = "every";
		public const string Intersect = "intersect";
		public const string Load = "load";
		public const string Revealed = "revealed";
		public const string Sse = "sse";
	}

	/// <summary>
	/// Htmx trigger modifier values for <c>hx-trigger</c>.
	/// </summary>
	public static class TriggerModifiers
	{
		public const string SseEvent = "sseEvent";
		public const string Once = "once";
		public const string Changed = "changed";
		public const string Delay = "delay";
		public const string Throttle = "throttle";
		public const string From = "from";
		public const string Target = "target";
		public const string Consume = "consume";
		public const string Queue = "queue";
		public const string Root = "root";
		public const string Threshold = "threshold";
	}

	/// <summary>
	/// Htmx swap style values.
	/// </summary>
	public static class SwapStyles
	{
		public const string InnerHTML = "innerHTML";
		public const string OuterHTML = "outerHTML";
		public const string BeforeBegin = "beforeBegin";
		public const string AfterBegin = "afterBegin";
		public const string BeforeEnd = "beforeEnd";
		public const string AfterEnd = "afterEnd";
		public const string Delete = "delete";
		public const string None = "none";
		public const string Default = "";
	}

	internal static class EventActionAttributes
	{
		public const string Get = "onget";
		public const string Post = "onpost";
		public const string Put = "onput";
		public const string Delete = "ondelete";
		public const string Patch = "onpatch";
	}
}
