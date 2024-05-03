namespace Htmxor;

internal static class HtmxConstants
{
	internal static class Attributes
	{
		internal const string Id = "id";
		internal const string Prefix = "hx-";

		internal const string HxGet = "hx-get";
		internal const string HxPost = "hx-post";
		internal const string HxPut = "hx-put";
		internal const string HxDelete = "hx-delete";
		internal const string HxPatch = "hx-patch";
		internal const string HxTrigger = "hx-trigger";
		internal const string HxTarget = "hx-target";
		internal const string HxSwap = "hx-swap";
		internal const string HxSwapOob = "hx-swap-oob";
		internal const string HxHeaders = "hx-headers";
	}

	internal static class Triggers
	{
		internal const string Load = "load";
	}

	internal static class SwapStyles
	{
		internal const string InnerHTML = "innerHTML";
		internal const string OuterHTML = "outerHTML";
		internal const string BeforeBegin = "beforebegin";
		internal const string AfterBegin = "afterbegin";
		internal const string BeforeEnd = "beforeend";
		internal const string AfterEnd = "afterend";
		internal const string Delete = "delete";
		internal const string None = "none";
		internal const string Default = "";
	}

	internal static class EventActionAttributes
	{
		internal const string Get = "onget";
		internal const string Post = "onpost";
		internal const string Put = "onput";
		internal const string Delete = "ondelete";
		internal const string Patch = "onpatch";
	}
}
