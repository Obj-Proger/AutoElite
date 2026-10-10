namespace AutoElite.Domain.Common;

/// <summary>
/// Represents a fact that has already happened within the domain, which other parts
/// of the system can react to without being called directly.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Gets the UTC timestamp when the event occurred.</summary>
    DateTime OccurredOnUtc { get; }
}
