namespace AutoElite.Application.Users.Authentication;

/// <summary>Creates signed access tokens; the infrastructure issues JSON Web Tokens.</summary>
public interface IAccessTokenFactory
{
    /// <summary>
    /// Creates an access token that carries the user's identifier, branch and roles, so that requests are authorized
    /// without reading the database.
    /// </summary>
    /// <param name="user">The signed-in user.</param>
    /// <param name="roles">The user's roles.</param>
    /// <param name="utcNow">The current UTC time, from which the lifetime is counted.</param>
    IssuedToken Create(User user, IReadOnlyCollection<UserRole> roles, DateTime utcNow);
}
