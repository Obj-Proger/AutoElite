using System.Text.Json;
using System.Text.Json.Serialization;
using AutoElite.Client.Authentication;

namespace AutoElite.Client;

/// <summary>
/// The JSON of what the client keeps itself, written by the source generator, so that it works in trimmed applications.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AuthSession))]
internal sealed partial class ClientJsonContext : JsonSerializerContext;
