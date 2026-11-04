namespace AutoElite.Application.Users;

/// <summary>
/// Reads users for queries. The infrastructure implements it with projections straight from the database, without
/// loading aggregates.
/// </summary>
public interface IUserQueries
{
    /// <summary>Reads the profile of a user, active or not.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The profile, or <see langword="null"/> if the user does not exist.</returns>
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
