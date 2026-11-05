namespace AutoElite.Infrastructure.Outbox;

/// <summary>
/// A domain event stored in the same transaction as the change that raised it, so that it is stored if and only if
/// the change is. The outbox processor delivers it to its handlers after the commit.
/// </summary>
internal sealed class OutboxMessage
{
    /// <summary>The longest name of an event type.</summary>
    public const int MaxTypeLength = 200;

    /// <summary>The longest error kept from a failed delivery.</summary>
    public const int MaxErrorLength = 4000;

    private OutboxMessage()
    {
    }

    /// <summary>Gets the identifier of the message, which also orders messages by creation.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the name of the event type; see <see cref="OutboxSerializer"/>.</summary>
    public string Type { get; private set; } = null!;

    /// <summary>Gets the event as JSON.</summary>
    public string Payload { get; private set; } = null!;

    /// <summary>Gets the UTC time the event occurred.</summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>Gets the UTC time the event was delivered, or <see langword="null"/> while it waits.</summary>
    public DateTime? ProcessedOnUtc { get; private set; }

    /// <summary>Gets the number of failed deliveries.</summary>
    public int Attempts { get; private set; }

    /// <summary>Gets the UTC time before which the message is not delivered: the pause after a failure.</summary>
    public DateTime NextAttemptOnUtc { get; private set; }

    /// <summary>Gets the error of the last failed delivery, for whoever investigates a message that keeps failing.</summary>
    public string? LastError { get; private set; }

    /// <summary>Creates the message of an event.</summary>
    /// <param name="domainEvent">The event.</param>
    public static OutboxMessage Create(IDomainEvent domainEvent) => new()
    {
        Id = Guid.CreateVersion7(),
        Type = OutboxSerializer.TypeNameOf(domainEvent),
        Payload = OutboxSerializer.Serialize(domainEvent),
        OccurredOnUtc = domainEvent.OccurredOnUtc,
        NextAttemptOnUtc = domainEvent.OccurredOnUtc
    };
}
