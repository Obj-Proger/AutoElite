namespace AutoElite.Infrastructure.Authentication;

/// <summary>
/// A refresh token as stored: only the hash of the value the client holds, so that a copy of the database cannot be
/// used to sign in. A token renews the session once; the new token replaces it.
/// </summary>
internal sealed class RefreshToken
{
    /// <summary>The length of the stored hash: SHA-256 in hexadecimal.</summary>
    public const int HashLength = 64;

    private RefreshToken()
    {
    }

    /// <summary>Gets the identifier of the token.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the user the token renews the session of.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the SHA-256 hash of the token's value.</summary>
    public string TokenHash { get; private set; } = null!;

    /// <summary>Gets the UTC time the token was issued.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Gets the UTC time after which the token is rejected.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>Gets the UTC time the token renewed the session, if it did.</summary>
    public DateTime? UsedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the token was revoked, if it was.</summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>Creates a token record.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="tokenHash">The hash of the value given to the client.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="lifetime">How long the token can be used.</param>
    public static RefreshToken Create(Guid userId, string tokenHash, DateTime utcNow, TimeSpan lifetime) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAtUtc = utcNow,
        ExpiresAtUtc = utcNow + lifetime
    };

    /// <summary>Determines whether the token can still renew the session: not used, revoked or expired.</summary>
    /// <param name="utcNow">The current UTC time.</param>
    public bool IsUsable(DateTime utcNow) => UsedAtUtc is null && RevokedAtUtc is null && utcNow < ExpiresAtUtc;

    /// <summary>Records that the token renewed the session.</summary>
    /// <param name="utcNow">The current UTC time.</param>
    public void MarkUsed(DateTime utcNow) => UsedAtUtc = utcNow;

    /// <summary>Revokes the token unless it was revoked already.</summary>
    /// <param name="utcNow">The current UTC time.</param>
    public void Revoke(DateTime utcNow) => RevokedAtUtc ??= utcNow;
}
