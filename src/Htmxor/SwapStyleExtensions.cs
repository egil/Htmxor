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
			SwapStyle.innerHTML => Constants.SwapStyles.InnerHTML,
			SwapStyle.outerHTML => Constants.SwapStyles.OuterHTML,
			SwapStyle.beforebegin => Constants.SwapStyles.BeforeBegin,
			SwapStyle.afterbegin => Constants.SwapStyles.AfterBegin,
			SwapStyle.beforeend => Constants.SwapStyles.BeforeEnd,
			SwapStyle.afterend => Constants.SwapStyles.AfterEnd,
			SwapStyle.delete => Constants.SwapStyles.Delete,
			SwapStyle.none => Constants.SwapStyles.None,
			SwapStyle.Default => Constants.SwapStyles.Default,
			_ => throw new SwitchExpressionException(swapStyle),
		};

		return style;
	}
}
