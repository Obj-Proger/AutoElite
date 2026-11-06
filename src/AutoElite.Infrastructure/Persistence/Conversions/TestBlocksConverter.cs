using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoElite.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores the blocks of a test template as a JSON array of <c>{"questionCount": n, "topicIds": [...]}</c>. The blocks
/// are value objects read and written with their template; a template has at most a handful of them.
/// </summary>
internal sealed class TestBlocksConverter() : ValueConverter<List<TestBlock>, string>(
    blocks => Write(blocks),
    json => Read(json))
{
    /// <summary>The name of the JSON property with the topic identifiers, which SQL searches for a topic.</summary>
    public const string TopicIdsProperty = "topicIds";

    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    /// <summary>Gets the comparer that tells the change tracker when the blocks changed, comparing them by value.</summary>
    public static ValueComparer<List<TestBlock>> Comparer { get; } = new(
        (left, right) => left!.SequenceEqual(right!),
        blocks => blocks.Aggregate(0, (hash, block) => HashCode.Combine(hash, block)),
        blocks => blocks.ToList());

    private static string Write(List<TestBlock> blocks) =>
        JsonSerializer.Serialize(
            blocks.Select(block => new StoredBlock(block.QuestionCount, [.. block.TopicIds])), _options);

    private static List<TestBlock> Read(string json) =>
        [.. JsonSerializer.Deserialize<StoredBlock[]>(json, _options)!
            .Select(block => TestBlock.Create(block.QuestionCount, block.TopicIds).Value)];

    /// <summary>A block as stored.</summary>
    private sealed record StoredBlock(int QuestionCount, Guid[] TopicIds);
}
