using System.Reflection;

namespace AutoElite.Application.Common.Authorization;

/// <summary>What a request type requires from the caller, read from its authorization attributes.</summary>
/// <param name="AllowsAnonymous">Whether anonymous callers may send the request.</param>
/// <param name="Roles">The roles of which the caller needs any one; empty if any signed-in user may send it.</param>
/// <param name="SystemOnly">Whether only the system may send the request.</param>
internal sealed record AuthorizationRequirement(bool AllowsAnonymous, IReadOnlyList<UserRole> Roles, bool SystemOnly)
{
    /// <summary>Reads the requirement of a request type.</summary>
    /// <param name="requestType">The type of the command or query.</param>
    /// <exception cref="InvalidOperationException">The request has more than one authorization attribute.</exception>
    public static AuthorizationRequirement For(Type requestType)
    {
        var allowsAnonymous = requestType.GetCustomAttribute<AllowAnonymousRequestAttribute>() is not null;
        var roles = requestType.GetCustomAttribute<RequireRolesAttribute>()?.Roles ?? [];
        var systemOnly = requestType.GetCustomAttribute<SystemRequestAttribute>() is not null;

        if (new[] { allowsAnonymous, roles.Count > 0, systemOnly }.Count(marked => marked) > 1)
        {
            throw new InvalidOperationException(
                $"{requestType.Name} must be either open to anonymous callers, restricted to roles or reserved for the system.");
        }

        return new AuthorizationRequirement(allowsAnonymous, roles, systemOnly);
    }
}
