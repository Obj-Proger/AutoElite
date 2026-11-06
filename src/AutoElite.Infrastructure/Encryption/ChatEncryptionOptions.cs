using System.Text.RegularExpressions;

namespace AutoElite.Infrastructure.Encryption;

/// <summary>
/// The keys that encrypt the text of chat messages, from the <see cref="SectionName"/> configuration section. The keys
/// are secrets: they are set with user secrets locally and with environment variables or a secret store when deployed
/// (<c>ChatEncryption__CurrentKeyId</c>, <c>ChatEncryption__Keys__{id}</c>), never in a committed file.
/// </summary>
/// <remarks>
/// To change the key, add a new one and make it current: new messages are encrypted with it, and the earlier keys stay
/// to read the messages they encrypted. A key can be removed only once no stored message uses it. A key is 32 random
/// bytes in Base64, for example from PowerShell's
/// <c>[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))</c>.
/// </remarks>
public sealed partial class ChatEncryptionOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "ChatEncryption";

    /// <summary>The length of a key: 256 bits, for AES-256.</summary>
    public const int KeyBytes = 32;

    /// <summary>The longest key identifier.</summary>
    public const int MaxKeyIdLength = 32;

    /// <summary>Gets or sets the identifier of the key that encrypts new messages; one of <see cref="Keys"/>.</summary>
    public string CurrentKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets the keys by identifier, each <see cref="KeyBytes"/> bytes in Base64. An identifier is up to
    /// <see cref="MaxKeyIdLength"/> Latin letters, digits and underscores, so that it can be part of an environment
    /// variable name; identifiers ignore case, as configuration keys do.
    /// </summary>
    public Dictionary<string, string> Keys { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (Keys.Count == 0)
        {
            yield return $"{SectionName}:Keys must hold at least one key. Set it with dotnet user-secrets locally or the "
                + $"{SectionName}__Keys__{{id}} environment variable when deployed.";
        }

        foreach (var (id, key) in Keys)
        {
            if (!KeyIdPattern().IsMatch(id))
            {
                yield return $"{SectionName}:Keys:{id}: the identifier must be 1 to {MaxKeyIdLength} Latin letters, "
                    + "digits or underscores.";
            }

            if (DecodeKey(key) is null)
                yield return $"{SectionName}:Keys:{id} must be {KeyBytes} random bytes in Base64.";
        }

        if (!Keys.ContainsKey(CurrentKeyId))
            yield return $"{SectionName}:CurrentKeyId must name one of the {SectionName}:Keys.";
    }

    /// <summary>Decodes a key from Base64.</summary>
    /// <returns>The key, or <see langword="null"/> if it is not Base64 of <see cref="KeyBytes"/> bytes.</returns>
    internal static byte[]? DecodeKey(string? key)
    {
        var bytes = new byte[KeyBytes];

        return Convert.TryFromBase64String(key ?? string.Empty, bytes, out var written) && written == KeyBytes ? bytes : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_]{1,32}$")]
    private static partial Regex KeyIdPattern();
}
