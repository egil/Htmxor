using System.Text.Json;
using System.Text.Json.Serialization;

namespace Htmxor.Serialization;

internal sealed class TimespanMillisecondJsonConverter : JsonConverter<TimeSpan?>
{
	public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType switch
		{
			JsonTokenType.Number => TimeSpan.FromMilliseconds(reader.GetInt32()),
			_ => null
		};

	public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
	{
		if (value.HasValue)
		{
			writer.WriteNumberValue((int)value.Value.TotalMilliseconds);
		}
	}
}
