namespace AutoElite.Application.TrainingGrounds;

/// <summary>Checks a training ground that a route or a driving slot starts from.</summary>
internal static class TrainingGroundChecks
{
    /// <summary>
    /// Checks that the ground, if any, exists inside the caller's scope and is in use. A ground outside the scope is
    /// reported as not found.
    /// </summary>
    /// <param name="groundRepository">The training ground repository.</param>
    /// <param name="groundId">The ground, or <see langword="null"/> for none.</param>
    /// <param name="scope">The caller's data scope.</param>
    /// <param name="inactiveError">The error to report for a ground that is no longer used.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The ground, <see langword="null"/> if none was given, or a failure.</returns>
    public static async Task<Result<TrainingGround?>> LoadAsync(
        IRepository<TrainingGround> groundRepository,
        Guid? groundId,
        DataScope scope,
        Error inactiveError,
        CancellationToken cancellationToken)
    {
        if (groundId is not { } id)
            return Result.Success<TrainingGround?>(null);

        var ground = await groundRepository.GetByIdAsync(id, cancellationToken);
        if (ground is null || !scope.Includes(ground.BranchId))
            return Result.Failure<TrainingGround?>(ApplicationErrors.NotFound<TrainingGround>());

        return ground.IsActive ? Result.Success<TrainingGround?>(ground) : Result.Failure<TrainingGround?>(inactiveError);
    }
}
