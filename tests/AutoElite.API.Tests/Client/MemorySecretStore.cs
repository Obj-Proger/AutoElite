using AutoElite.Client.Authentication;

namespace AutoElite.API.Tests.Client;

/// <summary>The secret store of a device, kept in memory, which counts its reads.</summary>
internal sealed class MemorySecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _secrets = [];

    /// <summary>Gets the number of reads.</summary>
    public int Reads { get; private set; }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key)
    {
        Reads++;

        return Task.FromResult(_secrets.GetValueOrDefault(key));
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value)
    {
        _secrets[key] = value;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool Remove(string key) => _secrets.Remove(key);
}
