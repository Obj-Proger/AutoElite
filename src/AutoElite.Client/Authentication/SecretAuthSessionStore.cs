using System.Text.Json;

namespace AutoElite.Client.Authentication;

/// <summary>
/// Keeps the session in a secret store, so that the user stays signed in when the application starts again, and in
/// memory once it is read, since every request needs it. A session that cannot be read, for example after a backup of
/// the device was restored without its keys, is forgotten.
/// </summary>
/// <param name="secrets">The secret store of the platform.</param>
public sealed class SecretAuthSessionStore(ISecretStore secrets) : IAuthSessionStore, IDisposable
{
    /// <summary>The key of the session in the secret store.</summary>
    public const string Key = "autoelite.session";

    private readonly SemaphoreSlim _loading = new(1, 1);
    private volatile Cached? _cached;

    /// <inheritdoc />
    public async ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is { } cached)
            return cached.Session;

        await _loading.WaitAsync(cancellationToken);
        try
        {
            if (_cached is { } loaded)
                return loaded.Session;

            var session = Read(await secrets.GetAsync(Key));
            _cached = new Cached(session);

            return session;
        }
        finally
        {
            _loading.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(AuthSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        await secrets.SetAsync(Key, JsonSerializer.Serialize(session, ClientJsonContext.Default.AuthSession));
        _cached = new Cached(session);
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken)
    {
        secrets.Remove(Key);
        _cached = new Cached(null);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => _loading.Dispose();

    private AuthSession? Read(string? json)
    {
        if (json is null)
            return null;

        try
        {
            return JsonSerializer.Deserialize(json, ClientJsonContext.Default.AuthSession);
        }
        catch (JsonException)
        {
            secrets.Remove(Key);

            return null;
        }
    }

    // The session as last read or kept, null included, so that a store without one is not read again and again.
    private sealed record Cached(AuthSession? Session);
}
