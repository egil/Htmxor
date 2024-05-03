using System.Text.Json.Serialization;
using Htmxor.Http;

namespace Htmxor.Serialization;

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	UseStringEnumConverter = true,
	GenerationMode = JsonSourceGenerationMode.Default,
	Converters = [typeof(TimespanMillisecondJsonConverter), typeof(JsonCamelCaseStringEnumConverter<SwapStyle>), typeof(JsonCamelCaseStringEnumConverter<ScrollBehavior>)])]
[JsonSerializable(typeof(HtmxConfig))]
[JsonSerializable(typeof(LocationTarget))]
[JsonSerializable(typeof(AjaxContext))]
internal sealed partial class HtmxJsonSerializerContext : JsonSerializerContext
{
}
