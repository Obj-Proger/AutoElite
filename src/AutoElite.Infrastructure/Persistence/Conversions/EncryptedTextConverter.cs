using AutoElite.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoElite.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores a text encrypted by <see cref="ChatTextCipher"/>. A missing text stays missing, so the column shows whether
/// there is a text, though not what it says.
/// </summary>
/// <remarks>
/// The model type is nullable to match the optional text, but EF Core never passes <see langword="null"/> to a
/// converter: a missing value is stored and read as <c>NULL</c> without it.
/// </remarks>
/// <param name="cipher">The cipher.</param>
internal sealed class EncryptedTextConverter(ChatTextCipher cipher) : ValueConverter<string?, string>(
    text => cipher.Encrypt(text!),
    stored => cipher.Decrypt(stored));
