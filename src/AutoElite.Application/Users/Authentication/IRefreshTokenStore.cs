namespace AutoElite.Application.Users.Authentication;

/// <summary>
/// Issues and checks refresh tokens. Only a hash of each token is stored, and every token can be used once:
/// redeeming it is part of rotating it.
/// </summary>
/// <remarks>
/// Issuing and redeeming are tracked changes saved by <see cref="IUnitOfWork"/>. When a token that was already used
/// is presented again, it was probably stolen: the implementation revokes all tokens of that user and saves this at
/// once, so the revocation holds even though the request fails.
/// </remarks>
public interface IRefreshTokenStore
{
    /// <summary>Issues a new refresh token for the user.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="utcNow">The current UTC time, from which the lifetime is counted.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IssuedToken> IssueAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Marks the token as used if it is valid.</summary>
    /// <param name="token">The token presented by the client.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The identifier of the token's user, or <see langword="null"/> if the token is unknown, expired, revoked or used.</returns>
    Task<Guid?> RedeemAsync(string token, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Revokes the token, for example when the user signs out. An unknown token is ignored.</summary>
    /// <param name="token">The token presented by the client.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task RevokeAsync(string token, DateTime utcNow, CancellationToken cancellationToken = default);
}
