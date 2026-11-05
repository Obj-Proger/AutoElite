using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoElite.Infrastructure.Outbox;

/// <summary>
/// Turns domain events into JSON and back. An event is stored under the name of its type; events are records of
/// identifiers, times, numbers, text and enumerations, which are written by name.
/// </summary>
internal static class OutboxSerializer
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly FrozenDictionary<string, Type> _eventTypes = typeof(IDomainEvent).Assembly.GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IDomainEvent)))
        .ToFrozenDictionary(type => type.Name, StringComparer.Ordinal);

    /// <summary>Gets the event types the outbox stores, by name.</summary>
    public static IReadOnlyCollection<Type> EventTypes => _eventTypes.Values;

    /// <summary>Gets the name an event is stored under.</summary>
    /// <param name="domainEvent">The event.</param>
    /// <exception cref="ArgumentException">The event is not a domain event of the domain assembly.</exception>
    public static string TypeNameOf(IDomainEvent domainEvent)
    {
        var name = domainEvent.GetType().Name;

        return _eventTypes.ContainsKey(name)
            ? name
            : throw new ArgumentException($"{name} is not a domain event the outbox can store.", nameof(domainEvent));
    }

    /// <summary>Writes an event as JSON.</summary>
    /// <param name="domainEvent">The event.</param>
    public static string Serialize(IDomainEvent domainEvent) =>
        JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _options);

    /// <summary>Reads an event stored under a type name.</summary>
    /// <param name="typeName">The name the event was stored under.</param>
    /// <param name="payload">The JSON of the event.</param>
    /// <exception cref="InvalidOperationException">The type is unknown.</exception>
    /// <exception cref="JsonException">The JSON is not an event of the type.</exception>
    public static IDomainEvent Deserialize(string typeName, string payload)
    {
        if (!_eventTypes.TryGetValue(typeName, out var type))
            throw new InvalidOperationException($"The outbox has no domain event type named \"{typeName}\".");

        return (IDomainEvent?)JsonSerializer.Deserialize(payload, type, _options)
            ?? throw new JsonException($"The stored {typeName} is empty.");
    }
}
