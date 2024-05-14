using System.Text.Json;
using System.Text.Json.Serialization;

namespace Htmxor.Serialization;

public sealed class SwapStyleJsonConverter : JsonConverter<SwapStyle>
{
	public override SwapStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();
		return SwapStyle.FromString(value ?? string.Empty);
	}

	public override void Write(Utf8JsonWriter writer, SwapStyle value, JsonSerializerOptions options)
	{
		if (value != null)
			writer?.WriteStringValue(value.Name);
	}
}
