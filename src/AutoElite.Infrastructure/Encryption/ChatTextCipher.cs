using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;

namespace AutoElite.Infrastructure.Encryption;

/// <summary>
/// Encrypts the text of chat messages for storage with AES-256-GCM (NFR-3.4), so that a copy of the database does not
/// disclose the correspondence without the keys, which are kept apart from it (<see cref="ChatEncryptionOptions"/>).
/// </summary>
/// <remarks>
/// The stored form is <c>{key id}:{Base64 of nonce, cipher text and tag}</c>: the key id tells which key decrypts it,
/// so that the current key can change while older messages stay readable. Each text gets a random 96-bit nonce, and
/// the tag makes any change to the stored form fail to decrypt. Encrypted text cannot be searched or compared by the
/// database.
/// </remarks>
public sealed class ChatTextCipher
{
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private const string Separator = ":";

    /// <summary>Binds the cipher texts to their purpose, so that a cipher text made for anything else does not decrypt.</summary>
    private static readonly byte[] _associatedData = "AutoElite.ChatMessage.Text"u8.ToArray();

    private readonly FrozenDictionary<string, byte[]> _keys;
    private readonly string _currentKeyId;

    /// <summary>Initializes a new instance of the <see cref="ChatTextCipher"/> class.</summary>
    /// <param name="options">The keys; they must be valid.</param>
    /// <exception cref="ArgumentException">The settings are not valid.</exception>
    public ChatTextCipher(ChatEncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        string[] problems = [.. options.Problems()];
        if (problems.Length > 0)
            throw new ArgumentException(string.Join(" ", problems), nameof(options));

        _keys = options.Keys.ToFrozenDictionary(
            pair => pair.Key, pair => ChatEncryptionOptions.DecodeKey(pair.Value)!, StringComparer.OrdinalIgnoreCase);
        _currentKeyId = options.Keys.Keys.First(
            id => string.Equals(id, options.CurrentKeyId, StringComparison.OrdinalIgnoreCase));
        Fingerprint = BuildFingerprint(_keys, _currentKeyId);
    }

    /// <summary>
    /// Gets a value that is equal for ciphers with the same keys and current key, so that such ciphers share one model
    /// of the database; it discloses nothing about the keys.
    /// </summary>
    internal string Fingerprint { get; }

    /// <summary>Encrypts a text with the current key.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The stored form.</returns>
    public string Encrypt(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var plain = Encoding.UTF8.GetBytes(text);
        var payload = new byte[NonceBytes + plain.Length + TagBytes];
        var nonce = payload.AsSpan(0, NonceBytes);
        RandomNumberGenerator.Fill(nonce);

        using (var aes = new AesGcm(_keys[_currentKeyId], TagBytes))
        {
            aes.Encrypt(
                nonce, plain, payload.AsSpan(NonceBytes, plain.Length), payload.AsSpan(NonceBytes + plain.Length),
                _associatedData);
        }

        return string.Concat(_currentKeyId, Separator, Convert.ToBase64String(payload));
    }

    /// <summary>Decrypts a stored text with the key it names.</summary>
    /// <param name="stored">The stored form made by <see cref="Encrypt"/>.</param>
    /// <returns>The text.</returns>
    /// <exception cref="CryptographicException">
    /// The stored form is malformed, its key is not configured, or it was changed after it was made.
    /// </exception>
    public string Decrypt(string stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        var separator = stored.IndexOf(Separator, StringComparison.Ordinal);
        if (separator <= 0)
            throw new CryptographicException("The stored chat text is not in the encrypted form.");

        var keyId = stored[..separator];
        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new CryptographicException(
                $"The chat text was encrypted with the key \"{keyId}\", which is not among "
                + $"{ChatEncryptionOptions.SectionName}:Keys.");
        }

        var payload = DecodePayload(stored[(separator + Separator.Length)..]);
        var plainLength = payload.Length - NonceBytes - TagBytes;
        var plain = new byte[plainLength];
        using (var aes = new AesGcm(key, TagBytes))
        {
            aes.Decrypt(
                payload.AsSpan(0, NonceBytes), payload.AsSpan(NonceBytes, plainLength), payload.AsSpan(NonceBytes + plainLength),
                plain, _associatedData);
        }

        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] DecodePayload(string encoded)
    {
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(encoded);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("The stored chat text is not in the encrypted form.", exception);
        }

        return payload.Length >= NonceBytes + TagBytes
            ? payload
            : throw new CryptographicException("The stored chat text is not in the encrypted form.");
    }

    private static string BuildFingerprint(FrozenDictionary<string, byte[]> keys, string currentKeyId)
    {
        var material = new StringBuilder(currentKeyId.ToUpperInvariant());
        foreach (var (id, key) in keys.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            material.Append(':').Append(id.ToUpperInvariant()).Append(':').Append(Convert.ToBase64String(key));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString())));
    }
}
