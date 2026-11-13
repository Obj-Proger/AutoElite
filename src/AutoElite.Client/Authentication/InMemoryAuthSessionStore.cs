namespace AutoElite.Client.Authentication;

/// <summary>Keeps the session in memory only, for clients that sign in again when they start, such as tools and tests.</summary>
public sealed class InMemoryAuthSessionStore : IAuthSessionStore
{
    private AuthSession? _session;

    /// <inheritdoc />
    public ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Volatile.Read(ref _session));

    /// <inheritdoc />
    public ValueTask SaveAsync(AuthSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        Volatile.Write(ref _session, session);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken)
    {
        Volatile.Write(ref _session, null);

        return ValueTask.CompletedTask;
    }
}
