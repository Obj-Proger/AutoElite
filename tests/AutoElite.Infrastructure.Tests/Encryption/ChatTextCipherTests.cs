using System.Security.Cryptography;
using AutoElite.Infrastructure.Encryption;

namespace AutoElite.Infrastructure.Tests.Encryption;

public sealed class ChatTextCipherTests
{
    private const string Text = "Встречаемся у автодрома в 9:00 🚗";

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(ChatEncryptionOptions.KeyBytes));

    [Fact]
    public void Decrypt_EncryptedText_ReturnsTheText()
    {
        var cipher = TestChatEncryption.Cipher;

        var stored = cipher.Encrypt(Text);

        stored.Should().StartWith(TestChatEncryption.KeyId + ":").And.NotContain("автодрома");
        cipher.Decrypt(stored).Should().Be(Text);
        cipher.Decrypt(cipher.Encrypt(string.Empty)).Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_SameTextTwice_GivesDifferentStoredForms()
    {
        var cipher = TestChatEncryption.Cipher;

        cipher.Encrypt(Text).Should().NotBe(cipher.Encrypt(Text));
    }

    [Fact]
    public void Decrypt_AfterTheCurrentKeyChanged_ReadsTextsOfTheOldKey()
    {
        var oldKey = NewKey();
        var before = new ChatTextCipher(TestChatEncryption.Options("k2026_01", ("k2026_01", oldKey)));
        var stored = before.Encrypt(Text);
        var after = new ChatTextCipher(TestChatEncryption.Options("k2026_07", ("k2026_01", oldKey), ("k2026_07", NewKey())));

        after.Decrypt(stored).Should().Be(Text);
        after.Encrypt(Text).Should().StartWith("k2026_07:");
    }

    [Fact]
    public void Decrypt_ChangedStoredForm_Throws()
    {
        var cipher = TestChatEncryption.Cipher;
        var stored = cipher.Encrypt(Text);
        var payload = Convert.FromBase64String(stored[(TestChatEncryption.KeyId.Length + 1)..]);
        payload[^1] ^= 0xFF;
        var changed = $"{TestChatEncryption.KeyId}:{Convert.ToBase64String(payload)}";

        var decrypt = () => cipher.Decrypt(changed);

        decrypt.Should().Throw<CryptographicException>();
    }

    [Theory]
    [InlineData("other_key:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("test_1:not base64")]
    [InlineData("test_1:AAAA")]
    [InlineData("plain text")]
    public void Decrypt_StoredFormItCannotRead_Throws(string stored)
    {
        var decrypt = () => TestChatEncryption.Cipher.Decrypt(stored);

        decrypt.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Constructor_InvalidSettings_ThrowsWithEveryProblem()
    {
        var options = TestChatEncryption.Options("missing", ("bad-id", NewKey()), ("short", "AQID"));

        var create = () => new ChatTextCipher(options);

        create.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("Keys:bad-id").And.Contain("Keys:short").And.Contain("CurrentKeyId");
    }

    [Fact]
    public void Constructor_NoKeys_Throws()
    {
        var create = () => new ChatTextCipher(TestChatEncryption.Options(string.Empty));

        create.Should().Throw<ArgumentException>().Which.Message.Should().Contain("at least one key");
    }

    [Fact]
    public void Fingerprint_DependsOnTheKeysAndTheCurrentKeyOnly()
    {
        var first = NewKey();
        var second = NewKey();

        var same = new ChatTextCipher(TestChatEncryption.Options("A", ("a", first), ("b", second)));
        var again = new ChatTextCipher(TestChatEncryption.Options("a", ("b", second), ("A", first)));
        var otherCurrent = new ChatTextCipher(TestChatEncryption.Options("b", ("a", first), ("b", second)));

        same.Fingerprint.Should().Be(again.Fingerprint);
        same.Fingerprint.Should().NotBe(otherCurrent.Fingerprint);
    }
}
