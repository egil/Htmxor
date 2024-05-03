using System.Runtime.CompilerServices;

namespace Htmxor;

public static class SwapStyleExtensions
{
	/// <summary>
	/// Converts the <paramref name="swapStyle"/> to their string version expected by htmx.
	/// </summary>
	/// <param name="swapStyle">The style to convert.</param>
	public static string ToHtmxString(this SwapStyle swapStyle)
	{
		var style = swapStyle switch
		{
			SwapStyle.InnerHTML => "innerHTML",
			SwapStyle.OuterHTML => "outerHTML",
			SwapStyle.BeforeBegin => "beforebegin",
			SwapStyle.AfterBegin => "afterbegin",
			SwapStyle.BeforeEnd => "beforeend",
			SwapStyle.AfterEnd => "afterend",
			SwapStyle.Delete => "delete",
			SwapStyle.None => "none",
			SwapStyle.Default => "",
			_ => throw new SwitchExpressionException(swapStyle),
		};

		return style;
	}
}