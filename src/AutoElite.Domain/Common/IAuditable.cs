namespace AutoElite.Domain.Common;

/// <summary>
/// Defines a contract for entities that track when and by whom they were created and last modified.
/// The values are populated by infrastructure when changes are saved, not by domain logic.
/// </summary>
public interface IAuditable
{
    /// <summary>Gets the UTC timestamp when the entity was created.</summary>
    DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity,
    /// or <see langword="null"/> if it was created by the system or by an anonymous visitor.
    /// </summary>
    Guid? CreatedByUserId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was last modified,
    /// or <see langword="null"/> if it has not been modified since creation.
    /// </summary>
    DateTime? ModifiedAtUtc { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified the entity,
    /// or <see langword="null"/> if it has not been modified or was modified by the system.
    /// </summary>
    Guid? ModifiedByUserId { get; }
}
