using System.Text;

namespace AutoElite.Infrastructure.Authentication;

/// <summary>
/// The settings of the session tokens, from the <see cref="SectionName"/> configuration section. The signing key is a
/// secret: it is set with user secrets locally and with the <c>Jwt__SigningKey</c> environment variable when deployed,
/// never in a committed file.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The shortest signing key: 256 bits, as HMAC-SHA256 requires.</summary>
    public const int MinSigningKeyBytes = 32;

    /// <summary>Gets or sets the issuer written into and expected in access tokens.</summary>
    public string Issuer { get; set; } = "AutoElite";

    /// <summary>Gets or sets the audience written into and expected in access tokens.</summary>
    public string Audience { get; set; } = "AutoElite";

    /// <summary>Gets or sets the key that signs access tokens; at least <see cref="MinSigningKeyBytes"/> bytes in UTF-8.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets how long an access token is accepted.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Gets or sets how long a refresh token can renew the session.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Gets the signing key as bytes.</summary>
    internal byte[] SigningKeyBytes => Encoding.UTF8.GetBytes(SigningKey);

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (SigningKeyBytes.Length < MinSigningKeyBytes)
        {
            yield return $"{SectionName}:SigningKey must be at least {MinSigningKeyBytes} bytes long. Set it with dotnet "
                + $"user-secrets locally or the {SectionName}__SigningKey environment variable when deployed.";
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            yield return $"{SectionName}:Issuer and {SectionName}:Audience must not be empty.";

        if (AccessTokenLifetime <= TimeSpan.Zero || RefreshTokenLifetime <= AccessTokenLifetime)
        {
            yield return $"{SectionName}:AccessTokenLifetime must be positive and shorter than "
                + $"{SectionName}:RefreshTokenLifetime.";
        }
    }
}
