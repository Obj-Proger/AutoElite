namespace AutoElite.Application.Users.GetUser;

/// <summary>Reads the card of a user for staff. Managers see the users of their branch and those without a branch.</summary>
/// <param name="UserId">The identifier of the user.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetUserQuery(Guid UserId) : IQuery<UserDetails>;
