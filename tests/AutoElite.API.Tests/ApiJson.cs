using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoElite.API.Tests;

/// <summary>The JSON of the API as a client reads it: camel case, enumerations as their names.</summary>
internal static class ApiJson
{
    /// <summary>Gets the options of the JSON of the API.</summary>
    public static JsonSerializerOptions Options { get; } =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
}
