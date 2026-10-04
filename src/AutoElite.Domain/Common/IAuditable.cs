namespace AutoElite.Domain.Common;

/// <summary>
/// Defines a contract for entities that track when they were created and last modified.
/// Implementing entities should declare both properties with a private setter — the values
/// are populated by infrastructure, not by domain logic.
/// </summary>
public interface IAuditable
{
    /// <summary>Gets the UTC timestamp when the entity was created.</summary>
    DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was last modified,
    /// or <see langword="null"/> if it has not been modified since creation.
    /// </summary>
    DateTime? ModifiedAtUtc { get; }
}