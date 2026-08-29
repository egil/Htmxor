using System.Text.Json.Serialization;
using Htmxor.Http;

namespace Htmxor.Serialization;

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	UseStringEnumConverter = true,
	GenerationMode = JsonSourceGenerationMode.Default,
	Converters = [
		typeof(SwapStyleEnumConverter),
	])]
[JsonSerializable(typeof(LocationTarget))]
[JsonSerializable(typeof(AjaxContext))]
internal sealed partial class HtmxorJsonSerializerContext : JsonSerializerContext
{
}
