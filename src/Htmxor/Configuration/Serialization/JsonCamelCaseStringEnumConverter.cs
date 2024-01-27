using System.Text.Json;
using System.Text.Json.Serialization;

namespace Htmxor.Configuration.Serialization;

internal sealed class JsonCamelCaseStringEnumConverter : JsonStringEnumConverter
{
    public JsonCamelCaseStringEnumConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
