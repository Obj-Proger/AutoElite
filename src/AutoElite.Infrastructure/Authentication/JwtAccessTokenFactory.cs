using System.Security.Claims;
using AutoElite.Application.Users.Authentication;
using AutoElite.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoElite.Infrastructure.Authentication;

/// <summary>
/// Creates signed JSON Web Tokens (HMAC-SHA256) with the user's identifier, branch and roles, so that requests are
/// authorized without reading the database. Roles changed later take effect with the next token.
/// </summary>
internal sealed class JwtAccessTokenFactory(IOptions<JwtOptions> options) : IAccessTokenFactory
{
    private static readonly JsonWebTokenHandler _handler = new();

    /// <inheritdoc />
    public IssuedToken Create(User user, IReadOnlyCollection<UserRole> roles, DateTime utcNow)
    {
        var settings = options.Value;
        var expiresAtUtc = utcNow + settings.AccessTokenLifetime;
        var claims = new List<Claim>
        {
            new(AccessTokenClaims.UserId, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        if (user.BranchId is { } branchId)
            claims.Add(new Claim(AccessTokenClaims.BranchId, branchId.ToString()));

        claims.AddRange(roles.Select(role => new Claim(AccessTokenClaims.Role, role.ToString())));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = utcNow,
            NotBefore = utcNow,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(SigningKey(settings), SecurityAlgorithms.HmacSha256)
        });

        return new IssuedToken(token, expiresAtUtc);
    }

    /// <summary>Gets the key that signs and checks access tokens.</summary>
    /// <param name="settings">The token settings.</param>
    internal static SymmetricSecurityKey SigningKey(JwtOptions settings) => new(settings.SigningKeyBytes);

    /// <summary>Gets the rules the host checks access tokens with; they match the tokens this factory creates.</summary>
    /// <param name="settings">The token settings.</param>
    internal static TokenValidationParameters ValidationParameters(JwtOptions settings) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = settings.Issuer,
        ValidateAudience = true,
        ValidAudience = settings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey(settings),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = AccessTokenClaims.UserId,
        RoleClaimType = AccessTokenClaims.Role
    };
}
