namespace AutoElite.Application.Users;

/// <summary>A user loaded for a staff action, with the roles the user holds.</summary>
/// <param name="User">The user.</param>
/// <param name="Roles">The roles of the user.</param>
internal sealed record ManagedUser(User User, IReadOnlyList<UserRole> Roles);
