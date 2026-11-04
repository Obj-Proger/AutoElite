namespace AutoElite.Application.TrainingGrounds;

/// <summary>Reads training grounds for queries.</summary>
public interface ITrainingGroundQueries
{
    /// <summary>
    /// Reads the grounds inside the scope, sorted by name. A school has few grounds, so the list is not paged.
    /// </summary>
    /// <param name="branchId">Only grounds of this branch; <see langword="null"/> for all.</param>
    /// <param name="isActive">Only grounds in use or only those no longer used; <see langword="null"/> for all.</param>
    /// <param name="scope">The branches the caller may see; shared grounds are in every scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<TrainingGroundItem>> GetListAsync(
        Guid? branchId, bool? isActive, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>Reads one ground.</summary>
    /// <param name="groundId">The identifier of the ground.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The ground, or <see langword="null"/> if it does not exist.</returns>
    Task<TrainingGroundItem?> GetAsync(Guid groundId, CancellationToken cancellationToken = default);
}
