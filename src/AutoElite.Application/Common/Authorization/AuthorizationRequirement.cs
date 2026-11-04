using System.Reflection;

namespace AutoElite.Application.Common.Authorization;

/// <summary>What a request type requires from the caller, read from its authorization attributes.</summary>
/// <param name="AllowsAnonymous">Whether anonymous callers may send the request.</param>
/// <param name="Roles">The roles of which the caller needs any one; empty if any signed-in user may send it.</param>
internal sealed record AuthorizationRequirement(bool AllowsAnonymous, IReadOnlyList<UserRole> Roles)
{
    /// <summary>Reads the requirement of a request type.</summary>
    /// <param name="requestType">The type of the command or query.</param>
    /// <exception cref="InvalidOperationException">The request is marked both anonymous and role-restricted.</exception>
    public static AuthorizationRequirement For(Type requestType)
    {
        var allowsAnonymous = requestType.GetCustomAttribute<AllowAnonymousRequestAttribute>() is not null;
        var roles = requestType.GetCustomAttribute<RequireRolesAttribute>()?.Roles ?? [];

        if (allowsAnonymous && roles.Count > 0)
        {
            throw new InvalidOperationException(
                $"{requestType.Name} cannot both allow anonymous callers and require roles.");
        }

        return new AuthorizationRequirement(allowsAnonymous, roles);
    }
}
