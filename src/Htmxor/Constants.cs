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
		public const string Load = "load";
	}

	/// <summary>
	/// Htmx swap style values.
	/// </summary>
	public static class SwapStyles
	{
		public const string InnerHTML = "innerHTML";
		public const string OuterHTML = "outerHTML";
		public const string BeforeBegin = "beforebegin";
		public const string AfterBegin = "afterbegin";
		public const string BeforeEnd = "beforeend";
		public const string AfterEnd = "afterend";
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
