using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private readonly Dictionary<string, StoredToken> _tokens = [];

    public IReadOnlyDictionary<string, StoredToken> Tokens => _tokens;

    public Task<IssuedToken> IssueAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var token = new IssuedToken($"refresh-{_tokens.Count + 1}", utcNow + Lifetime);
        _tokens.Add(token.Value, new StoredToken(userId, token.ExpiresAtUtc));

        return Task.FromResult(token);
    }

    public Task<Guid?> RedeemAsync(string token, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryGetValue(token, out var stored) || stored.IsRevoked || stored.ExpiresAtUtc <= utcNow)
            return Task.FromResult<Guid?>(null);

        if (stored.IsUsed)
        {
            foreach (var other in _tokens.Values.Where(other => other.UserId == stored.UserId))
                other.IsRevoked = true;

            return Task.FromResult<Guid?>(null);
        }

        stored.IsUsed = true;

        return Task.FromResult<Guid?>(stored.UserId);
    }

    public Task RevokeAsync(string token, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(token, out var stored))
            stored.IsRevoked = true;

        return Task.CompletedTask;
    }

    public Task RevokeAllAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        foreach (var stored in _tokens.Values.Where(stored => stored.UserId == userId))
            stored.IsRevoked = true;

        return Task.CompletedTask;
    }

    internal sealed class StoredToken(Guid userId, DateTime expiresAtUtc)
    {
        public Guid UserId { get; } = userId;

        public DateTime ExpiresAtUtc { get; } = expiresAtUtc;

        public bool IsUsed { get; set; }

        public bool IsRevoked { get; set; }
    }
}
