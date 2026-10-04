namespace AutoElite.Domain.Common;

/// <summary>
/// Defines a contract for entities that raise domain events — facts that already happened
/// and that other parts of the system can react to without being called directly.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Gets the domain events raised by this entity that have not yet been dispatched.</summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>Clears all domain events after they have been dispatched.</summary>
    void ClearDomainEvents();
}