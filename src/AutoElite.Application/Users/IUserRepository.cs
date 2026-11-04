namespace AutoElite.Application.Users;

/// <summary>Loads and adds users for commands.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Determines whether any user, active or not, already has the email address.</summary>
    /// <param name="email">The normalized email address.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Loads the user with the email address, active or not.</summary>
    /// <param name="email">The normalized email address.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The user, or <see langword="null"/> if no user has the address.</returns>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
}
