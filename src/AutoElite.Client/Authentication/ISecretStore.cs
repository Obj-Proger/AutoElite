namespace AutoElite.Client.Authentication;

/// <summary>
/// Keeps short secrets encrypted by the platform, such as the secure storage of a device (MAUI <c>ISecureStorage</c>,
/// whose members these are) or the protected storage of a browser.
/// </summary>
public interface ISecretStore
{
    /// <summary>Reads a secret.</summary>
    /// <param name="key">The key of the secret.</param>
    /// <returns>The secret, or <see langword="null"/> when there is none.</returns>
    Task<string?> GetAsync(string key);

    /// <summary>Keeps a secret in place of the previous one.</summary>
    /// <param name="key">The key of the secret.</param>
    /// <param name="value">The secret.</param>
    Task SetAsync(string key, string value);

    /// <summary>Forgets a secret.</summary>
    /// <param name="key">The key of the secret.</param>
    /// <returns><see langword="true"/> when there was one.</returns>
    bool Remove(string key);
}
