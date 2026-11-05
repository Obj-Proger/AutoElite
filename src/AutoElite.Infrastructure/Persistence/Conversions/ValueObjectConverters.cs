using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AutoElite.Infrastructure.Persistence.Conversions;

/// <summary>Stores a <see cref="Currency"/> as its ISO 4217 code.</summary>
/// <remarks>Stored values were valid when written, so reading one back through the factory cannot fail.</remarks>
internal sealed class CurrencyConverter() : ValueConverter<Currency, string>(
    currency => currency.Code,
    code => Currency.Create(code).Value);

/// <summary>Stores a <see cref="PhoneNumber"/> in its normalized E.164 form.</summary>
internal sealed class PhoneNumberConverter() : ValueConverter<PhoneNumber, string>(
    phone => phone.Value,
    value => PhoneNumber.Create(value).Value);

/// <summary>Stores an <see cref="Email"/> in its normalized, lower-case form.</summary>
internal sealed class EmailConverter() : ValueConverter<Email, string>(
    email => email.Value,
    value => Email.Create(value).Value);
