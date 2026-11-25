using System.Text;

namespace AutoElite.Mobile.Pages;

/// <summary>Phone numbers as people type them, turned into the international format the school keeps.</summary>
internal static class PhoneNumbers
{
    /// <summary>
    /// Turns a typed number into the E.164 format: spaces, hyphens, dots and brackets are dropped, a leading 00 becomes
    /// the plus sign, and an eleven-digit number that starts with 8, as numbers are dialled in Russia and Kazakhstan,
    /// becomes +7. Any other number without the plus sign is taken to start with its country code.
    /// </summary>
    /// <param name="text">The number as typed.</param>
    /// <returns>The number, or an empty string when nothing was typed.</returns>
    public static string Normalize(string? text)
    {
        var builder = new StringBuilder();
        foreach (var character in (text ?? string.Empty).Trim())
        {
            if (char.IsAsciiDigit(character) || (character == '+' && builder.Length == 0))
                builder.Append(character);
        }

        var number = builder.ToString();
        if (number.Length == 0 || number.StartsWith('+'))
            return number;

        if (number.StartsWith("00", StringComparison.Ordinal))
            return "+" + number[2..];

        return number.Length == 11 && number[0] == '8' ? "+7" + number[1..] : "+" + number;
    }
}
