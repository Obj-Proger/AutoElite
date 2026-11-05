namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// The user on whose behalf the current request runs. The API layer implements it from the access token;
/// background jobs use the system implementation, which sends only requests marked with
/// <see cref="SystemRequestAttribute"/>.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Gets the identifier of the signed-in user, or <see langword="null"/> for an anonymous caller.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the branch the user is tied to, or <see langword="null"/> if the user works for the whole school.
    /// Managers see only the data of their own branch.
    /// </summary>
    Guid? BranchId { get; }

    /// <summary>Gets the roles of the signed-in user; empty for an anonymous caller.</summary>
    IReadOnlySet<UserRole> Roles { get; }

    /// <summary>
    /// Gets a value indicating whether the request is sent by the system itself, such as a scheduled job, rather than
    /// by a person. The system has no user identifier and no roles.
    /// </summary>
    bool IsSystem { get; }
}
