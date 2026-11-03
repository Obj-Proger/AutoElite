using AutoElite.Domain.Enums;

namespace AutoElite.Application.Common.Abstractions;

/// <summary>Convenience checks on <see cref="ICurrentUser"/>.</summary>
public static class CurrentUserExtensions
{
    /// <summary>Determines whether a user is signed in.</summary>
    /// <param name="currentUser">The current user.</param>
    public static bool IsAuthenticated(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.UserId is not null;
    }

    /// <summary>
    /// Gets the identifier of the signed-in user. Use it in handlers of requests that require authorization,
    /// where the pipeline has already rejected anonymous callers.
    /// </summary>
    /// <param name="currentUser">The current user.</param>
    /// <exception cref="InvalidOperationException">Nobody is signed in, so the request is missing its authorization requirement.</exception>
    public static Guid GetRequiredUserId(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.UserId
            ?? throw new InvalidOperationException(
                "The request needs a signed-in user, but the caller is anonymous. Mark the request as requiring authorization.");
    }

    /// <summary>Determines whether the user has the specified role.</summary>
    /// <param name="currentUser">The current user.</param>
    /// <param name="role">The role to check.</param>
    public static bool IsInRole(this ICurrentUser currentUser, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        return currentUser.Roles.Contains(role);
    }

    /// <summary>Determines whether the user has at least one of the specified roles.</summary>
    /// <param name="currentUser">The current user.</param>
    /// <param name="roles">The roles to check.</param>
    public static bool IsInAnyRole(this ICurrentUser currentUser, params ReadOnlySpan<UserRole> roles)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        foreach (var role in roles)
        {
            if (currentUser.Roles.Contains(role))
                return true;
        }

        return false;
    }
}
