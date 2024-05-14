using System.Linq;
using System.Text.Json.Serialization;
using Htmxor.Serialization;

namespace Htmxor;

/// <summary>
/// How to swap the response into the target element.
/// </summary>
[JsonConverter(typeof(SwapStyleJsonConverter))]
public class SwapStyle
{
	internal string Name { get; }
	private readonly int value;

	private SwapStyle(string name, int value)
	{
		Name = name;
		this.value = value;
	}

	/// <summary>
	/// Default style is what is specified in <see cref="HtmxConfig.DefaultSwapStyle"/> for the application
	/// or htmx's default, which is <see cref="SwapStyle.InnerHTML"/>.
	/// </summary>
	public static readonly SwapStyle Default = new SwapStyle(Constants.SwapStyles.Default, 1);

	/// <summary>
	/// Replace the inner html of the target element.
	/// </summary>
	public static readonly SwapStyle InnerHTML = new SwapStyle(Constants.SwapStyles.InnerHTML, 2);

	/// <summary>
	/// Replace the entire target element with the response.
	/// </summary>
	public static readonly SwapStyle OuterHTML = new SwapStyle(Constants.SwapStyles.OuterHTML, 3);

	/// <summary>
	/// Insert the response before the target element.
	/// </summary>
	public static readonly SwapStyle BeforeBegin = new SwapStyle(Constants.SwapStyles.BeforeBegin, 4);

	/// <summary>
	/// Insert the response before the first child of the target element.
	/// </summary>
	public static readonly SwapStyle AfterBegin = new SwapStyle(Constants.SwapStyles.AfterBegin, 5);

	/// <summary>
	/// Insert the response after the last child of the target element.
	/// </summary>
	public static readonly SwapStyle BeforeEnd = new SwapStyle(Constants.SwapStyles.BeforeEnd, 6);

	/// <summary>
	/// Insert the response after the target element.
	/// </summary>
	public static readonly SwapStyle AfterEnd = new SwapStyle(Constants.SwapStyles.AfterEnd, 7);

	/// <summary>
	/// Deletes the target element regardless of the response.
	/// </summary>
	public static readonly SwapStyle Delete = new SwapStyle(Constants.SwapStyles.Delete, 8);

	/// <summary>
	/// Does not append content from response (out of band items will still be processed).
	/// </summary>
	public static readonly SwapStyle None = new SwapStyle(Constants.SwapStyles.None, 9);

	private static readonly Dictionary<string, SwapStyle> Lookup = new Dictionary<string, SwapStyle>
	{
		{ Constants.SwapStyles.Default, Default },
		{ Constants.SwapStyles.InnerHTML, InnerHTML },
		{ Constants.SwapStyles.OuterHTML, OuterHTML },
		{ Constants.SwapStyles.BeforeBegin, BeforeBegin },
		{ Constants.SwapStyles.AfterBegin, AfterBegin },
		{ Constants.SwapStyles.BeforeEnd, BeforeEnd },
		{ Constants.SwapStyles.AfterEnd, AfterEnd },
		{ Constants.SwapStyles.Delete, Delete },
		{ Constants.SwapStyles.None, None }
	};

	public override string ToString() => Name;

	public static SwapStyle FromString(string name)
	{
		if (Lookup.TryGetValue(name, out var swapStyle))
		{
			return swapStyle;
		}

		throw new ArgumentException("Invalid SwapStyle name", nameof(name));
	}

	public static implicit operator SwapStyle(string name) => FromString(name);

	public static implicit operator string(SwapStyle swapStyle)
	{
		ArgumentNullException.ThrowIfNull(swapStyle!);

		return swapStyle.Name;
	}

	// Override the == operator
	public static bool operator ==(SwapStyle? left, SwapStyle? right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}

		// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (left is null && right is null)
		{
			return true;
		}

		if (left is null || right is null)
		{
			return false;
		}
		// ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

		return left.value == right.value;
	}

	// Override the != operator
	public static bool operator != (SwapStyle? left, SwapStyle? right)
	{
		return !(left == right);
	}

	// Override the Equals method
	public override bool Equals(object? obj)
	{
		if (obj is SwapStyle other)
		{
			return value == other.value;
		}

		return false;
	}

	// Override the GetHashCode method
	public override int GetHashCode()
	{
		return HashCode.Combine(Name, value);
	}
}
