using AutoElite.Infrastructure.Encryption;

namespace AutoElite.Infrastructure.Tests.TestDoubles;

/// <summary>The chat encryption keys of the tests: fixed, so that every context of a run shares one model.</summary>
internal static class TestChatEncryption
{
    /// <summary>The identifier of the only key.</summary>
    public const string KeyId = "test_1";

    /// <summary>The key: 32 bytes of 0x01 in Base64.</summary>
    public const string Key = "AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=";

    /// <summary>Gets the cipher with the key.</summary>
    public static ChatTextCipher Cipher { get; } = new(Options(KeyId, (KeyId, Key)));

    /// <summary>Gets the configuration entries of the key, for tests that build the services from configuration.</summary>
    public static IReadOnlyDictionary<string, string?> Configuration { get; } = new Dictionary<string, string?>
    {
        [$"{ChatEncryptionOptions.SectionName}:CurrentKeyId"] = KeyId,
        [$"{ChatEncryptionOptions.SectionName}:Keys:{KeyId}"] = Key
    };

    /// <summary>Builds settings with the given keys.</summary>
    /// <param name="currentKeyId">The key that encrypts.</param>
    /// <param name="keys">The keys by identifier.</param>
    public static ChatEncryptionOptions Options(string currentKeyId, params (string Id, string Key)[] keys)
    {
        var options = new ChatEncryptionOptions { CurrentKeyId = currentKeyId };
        foreach (var (id, key) in keys)
            options.Keys.Add(id, key);

        return options;
    }
}
