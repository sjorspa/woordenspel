using System.Text.Json;
using System.Text.Json.Serialization;

namespace Woordenspel.Contracts;

/// <summary>De JSON-instellingen van de API: camelCase en enums als tekst.</summary>
public static class ContractJson
{
    public static JsonSerializerOptions Options { get; } = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
