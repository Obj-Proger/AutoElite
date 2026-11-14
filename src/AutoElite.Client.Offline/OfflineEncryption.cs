using System.Security.Cryptography;
using System.Text;
using AutoElite.Client.Authentication;

namespace AutoElite.Client.Offline;

/// <summary>
/// Encrypts what the offline database keeps with AES-GCM under a key of 256 bits that lives in the secret store of the
/// platform, apart from the file, so that a copy of the file alone, taken from the device or from a backup, cannot be
/// read. The key is made when there is none and forgotten with the data, so that what was kept before cannot be read
/// again. Each content is bound to the place it is kept in, so that it cannot be moved to another place unnoticed.
/// </summary>
/// <param name="secrets">The secret store of the platform.</param>
internal sealed class OfflineEncryption(ISecretStore secrets)
{
    /// <summary>The key of the encryption key in the secret store.</summary>
    public const string KeyName = "autoelite.offline-key";

    private const string NotSupported = "AES-GCM, which encrypts the offline data, is not supported on this platform.";

    // A content is the version of its format, a random nonce, the tag of AES-GCM and the encrypted text.
    private const byte Format = 1;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int TagOffset = 1 + NonceSize;
    private const int HeaderSize = TagOffset + TagSize;

    private byte[]? _key;

    /// <summary>Reads the key from the secret store, or makes one when there is none or it cannot be read.</summary>
    /// <returns><see langword="true"/> when the key was made, so that nothing kept before can be read with it.</returns>
    public async Task<bool> LoadAsync()
    {
        if (_key is not null)
            return false;

        if (await secrets.GetAsync(KeyName) is { } saved && TryDecode(saved, out var key))
        {
            _key = key;

            return false;
        }

        var made = RandomNumberGenerator.GetBytes(KeySize);
        await secrets.SetAsync(KeyName, Convert.ToBase64String(made));
        _key = made;

        return true;
    }

    /// <summary>Forgets the key, in memory and in the secret store; the next <see cref="LoadAsync"/> makes another.</summary>
    public void Forget()
    {
        if (_key is { } key)
            CryptographicOperations.ZeroMemory(key);

        _key = null;
        secrets.Remove(KeyName);
    }

    /// <summary>Encrypts a content.</summary>
    /// <param name="content">The content.</param>
    /// <param name="place">The place the content is kept in, which only it can be read from.</param>
    /// <returns>The encrypted content.</returns>
    public byte[] Encrypt(string content, string place)
    {
        if (!AesGcm.IsSupported)
            throw new PlatformNotSupportedException(NotSupported);

        var key = _key ?? throw new InvalidOperationException("The key of the offline data is not loaded.");
        var text = Encoding.UTF8.GetBytes(content);
        var encrypted = new byte[HeaderSize + text.Length];
        encrypted[0] = Format;
        var nonce = encrypted.AsSpan(1, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, text, encrypted.AsSpan(HeaderSize), encrypted.AsSpan(TagOffset, TagSize), Encoding.UTF8.GetBytes(place));

        return encrypted;
    }

    /// <summary>Decrypts a content.</summary>
    /// <param name="encrypted">The encrypted content.</param>
    /// <param name="place">The place the content was read from.</param>
    /// <returns>
    /// The content, or <see langword="null"/> when it cannot be read: it was encrypted with another key, belongs to
    /// another place or was changed.
    /// </returns>
    public string? Decrypt(byte[] encrypted, string place)
    {
        if (!AesGcm.IsSupported)
            throw new PlatformNotSupportedException(NotSupported);

        if (_key is not { } key || encrypted.Length < HeaderSize || encrypted[0] != Format)
            return null;

        var text = new byte[encrypted.Length - HeaderSize];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(
                encrypted.AsSpan(1, NonceSize),
                encrypted.AsSpan(HeaderSize),
                encrypted.AsSpan(TagOffset, TagSize),
                text,
                Encoding.UTF8.GetBytes(place));
        }
        catch (CryptographicException)
        {
            return null;
        }

        return Encoding.UTF8.GetString(text);
    }

    private static bool TryDecode(string saved, out byte[] key)
    {
        key = new byte[KeySize];

        return Convert.TryFromBase64String(saved, key, out var written) && written == KeySize;
    }
}
