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
			SwapStyle.InnerHTML => Constants.SwapStyles.InnerHTML,
			SwapStyle.OuterHTML => Constants.SwapStyles.OuterHTML,
			SwapStyle.BeforeBegin => Constants.SwapStyles.BeforeBegin,
			SwapStyle.AfterBegin => Constants.SwapStyles.AfterBegin,
			SwapStyle.BeforeEnd => Constants.SwapStyles.BeforeEnd,
			SwapStyle.AfterEnd => Constants.SwapStyles.AfterEnd,
			SwapStyle.Delete => Constants.SwapStyles.Delete,
			SwapStyle.None => Constants.SwapStyles.None,
			SwapStyle.Default => Constants.SwapStyles.Default,
			_ => throw new SwitchExpressionException(swapStyle),
		};

		return style;
	}
}
