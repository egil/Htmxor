using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Htmxor.Serialization;

internal sealed class SwapStyleEnumConverter : JsonConverter<SwapStyle>
{
	public override SwapStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();

		return value switch
		{
			null or "" => SwapStyle.Default,
			"innerHTML" => SwapStyle.innerHTML,
			"outerHTML" => SwapStyle.outerHTML,
			"beforebegin" => SwapStyle.beforebegin,
			"afterbegin" => SwapStyle.afterbegin,
			"beforeend" => SwapStyle.beforeend,
			"afterend" => SwapStyle.afterend,
			"delete" => SwapStyle.delete,
			"none" => SwapStyle.none,
			_ => throw new SwitchExpressionException(value),
		};
	}

	public override void Write(Utf8JsonWriter writer, SwapStyle value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToHtmxString());
	}
}
