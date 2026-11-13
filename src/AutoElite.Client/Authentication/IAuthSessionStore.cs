namespace AutoElite.Client.Authentication;

/// <summary>
/// Keeps the session of the signed-in user between requests and, in an application, between its starts: secure storage
/// on a device, protected storage in a browser.
/// </summary>
public interface IAuthSessionStore
{
    /// <summary>Reads the session.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The session, or <see langword="null"/> when nobody is signed in.</returns>
    ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken);

    /// <summary>Keeps a new or renewed session in place of the previous one.</summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    ValueTask SaveAsync(AuthSession session, CancellationToken cancellationToken);

    /// <summary>Forgets the session.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    ValueTask ClearAsync(CancellationToken cancellationToken);
}
