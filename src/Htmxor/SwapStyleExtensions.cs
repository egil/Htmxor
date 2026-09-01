using System.Runtime.CompilerServices;

namespace Htmxor;

internal static class SwapStyleExtensions
{
	public static string ToHtmxString(this SwapStyle swapStyle)
	{
		return swapStyle switch
		{
			SwapStyle.innerHTML => "innerHTML",
			SwapStyle.outerHTML => "outerHTML",
			SwapStyle.beforebegin => "beforebegin",
			SwapStyle.afterbegin => "afterbegin",
			SwapStyle.beforeend => "beforeend",
			SwapStyle.afterend => "afterend",
			SwapStyle.delete => "delete",
			SwapStyle.none => "none",
			SwapStyle.Default => "",
			_ => throw new SwitchExpressionException(swapStyle),
		};
	}
}
