using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Htmxor.Serialization;

internal sealed class SwapStyleEnumConverter : JsonConverter<SwapStyle>
{
	public override SwapStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();

		var style = value switch
		{
			null => SwapStyle.Default,
			Constants.SwapStyles.Default => SwapStyle.Default,
			Constants.SwapStyles.InnerHTML => SwapStyle.InnerHTML,
			Constants.SwapStyles.OuterHTML => SwapStyle.OuterHTML,
			Constants.SwapStyles.BeforeBegin => SwapStyle.BeforeBegin,
			Constants.SwapStyles.AfterBegin => SwapStyle.AfterBegin,
			Constants.SwapStyles.BeforeEnd => SwapStyle.BeforeEnd,
			Constants.SwapStyles.AfterEnd => SwapStyle.AfterEnd,
			Constants.SwapStyles.Delete => SwapStyle.Delete,
			Constants.SwapStyles.None => SwapStyle.None,
			_ => throw new SwitchExpressionException(value)
		};

		return style;
	}

	public override void Write(Utf8JsonWriter writer, SwapStyle value, JsonSerializerOptions options)
	{
		writer?.WriteStringValue(value.ToHtmxString());
	}
}
