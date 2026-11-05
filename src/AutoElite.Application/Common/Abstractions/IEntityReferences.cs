namespace AutoElite.Application.Common.Abstractions;

/// <summary>Checks references to records of any kind, such as the record a staff task or a document is about.</summary>
public interface IEntityReferences
{
    /// <summary>Determines whether the record exists; a deactivated record still exists.</summary>
    /// <param name="reference">The reference to check.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> ExistsAsync(EntityReference reference, CancellationToken cancellationToken = default);
}
