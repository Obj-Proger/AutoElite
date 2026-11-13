using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoElite.Client.Offline;

/// <summary>
/// The JSON of the actions in the queue, written by the source generator, so that it works in trimmed applications;
/// enumerations are kept by name, which does not depend on the order the client generator gives their values.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(OfflineAction))]
internal sealed partial class OfflineJsonContext : JsonSerializerContext;
