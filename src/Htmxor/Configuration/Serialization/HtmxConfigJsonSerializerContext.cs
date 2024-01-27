using System.Text.Json.Serialization;

namespace Htmxor.Configuration.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(JsonCamelCaseStringEnumConverter), typeof(TimespanMillisecondJsonConverter)])]
[JsonSerializable(typeof(HtmxConfig))]
internal sealed partial class HtmxConfigJsonSerializerContext : JsonSerializerContext
{
}
