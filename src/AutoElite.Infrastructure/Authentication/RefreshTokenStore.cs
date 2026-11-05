using System.Security.Cryptography;
using System.Text;
using AutoElite.Application.Users.Authentication;
using AutoElite.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Authentication;

/// <summary>
/// Issues and redeems refresh tokens. A token is 256 random bits; only its SHA-256 hash is stored, and the lookup is by
/// that hash. The changes are tracked and saved by the unit of work, except the revocation after reuse.
/// </summary>
/// <remarks>
/// A token presented after it was used is a sign that it was stolen, because the rightful client got a new one. All
/// of the user's tokens are then revoked straight in the database, so the revocation holds although the request fails.
/// Two requests that redeem the same token at the same time conflict on the row version of the token, so only one
/// of them gets new tokens.
/// </remarks>
internal sealed class RefreshTokenStore(AutoEliteDbContext dbContext, IOptions<JwtOptions> options) : IRefreshTokenStore
{
    private const int TokenBytes = 32;

    private DbSet<RefreshToken> Tokens => dbContext.Set<RefreshToken>();

    /// <inheritdoc />
    public Task<IssuedToken> IssueAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var value = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        var token = RefreshToken.Create(userId, Hash(value), utcNow, options.Value.RefreshTokenLifetime);
        Tokens.Add(token);

        return Task.FromResult(new IssuedToken(value, token.ExpiresAtUtc));
    }

    /// <inheritdoc />
    public async Task<Guid?> RedeemAsync(string token, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var stored = await FindAsync(token, cancellationToken);
        if (stored is null)
            return null;

        if (stored.UsedAtUtc is not null)
        {
            await RevokeAllNowAsync(stored.UserId, utcNow, cancellationToken);

            return null;
        }

        if (!stored.IsUsable(utcNow))
            return null;

        stored.MarkUsed(utcNow);

        return stored.UserId;
    }

    /// <inheritdoc />
    public async Task RevokeAsync(string token, DateTime utcNow, CancellationToken cancellationToken = default) =>
        (await FindAsync(token, cancellationToken))?.Revoke(utcNow);

    /// <inheritdoc />
    public async Task RevokeAllAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var active = await Tokens
            .Where(stored => stored.UserId == userId && stored.RevokedAtUtc == null && stored.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var stored in active)
            stored.Revoke(utcNow);
    }

    private Task<RefreshToken?> FindAsync(string token, CancellationToken cancellationToken)
    {
        var hash = Hash(token);

        return Tokens.FirstOrDefaultAsync(stored => stored.TokenHash == hash, cancellationToken);
    }

    private Task<int> RevokeAllNowAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken) =>
        Tokens
            .Where(stored => stored.UserId == userId && stored.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(stored => stored.RevokedAtUtc, (DateTime?)utcNow), cancellationToken);

    private static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
