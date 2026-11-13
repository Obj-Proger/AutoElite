using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoElite.Client.Offline;

/// <summary>
/// The JSON of the actions in the queue, written by the source generator, so that it works in trimmed applications.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OfflineAction))]
internal sealed partial class OfflineJsonContext : JsonSerializerContext;
