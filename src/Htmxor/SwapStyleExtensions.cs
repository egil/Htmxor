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
			SwapStyle.InnerHTML => HtmxConstants.SwapStyles.InnerHTML,
			SwapStyle.OuterHTML => HtmxConstants.SwapStyles.OuterHTML,
			SwapStyle.BeforeBegin => HtmxConstants.SwapStyles.BeforeBegin,
			SwapStyle.AfterBegin => HtmxConstants.SwapStyles.AfterBegin,
			SwapStyle.BeforeEnd => HtmxConstants.SwapStyles.BeforeEnd,
			SwapStyle.AfterEnd => HtmxConstants.SwapStyles.AfterEnd,
			SwapStyle.Delete => HtmxConstants.SwapStyles.Delete,
			SwapStyle.None => HtmxConstants.SwapStyles.None,
			SwapStyle.Default => HtmxConstants.SwapStyles.Default,
			_ => throw new SwitchExpressionException(swapStyle),
		};

		return style;
	}
}
