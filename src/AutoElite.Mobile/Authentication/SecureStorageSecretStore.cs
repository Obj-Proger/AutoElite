using AutoElite.Client.Authentication;

namespace AutoElite.Mobile.Authentication;

/// <summary>The secret store of the client on the secure storage of the device: the Keystore of Android, DPAPI on Windows.</summary>
/// <param name="secureStorage">The secure storage of MAUI.</param>
internal sealed class SecureStorageSecretStore(ISecureStorage secureStorage) : ISecretStore
{
    /// <inheritdoc />
    public Task<string?> GetAsync(string key) => secureStorage.GetAsync(key);

    /// <inheritdoc />
    public Task SetAsync(string key, string value) => secureStorage.SetAsync(key, value);

    /// <inheritdoc />
    public bool Remove(string key) => secureStorage.Remove(key);
}
