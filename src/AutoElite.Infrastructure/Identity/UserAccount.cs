using Microsoft.AspNetCore.Identity;

namespace AutoElite.Infrastructure.Identity;

/// <summary>
/// The sign-in account of a <see cref="User"/>: the password hash, email confirmation, lockout and the security stamp
/// that the tokens sent by email depend on. It shares its identifier with the user; the user is the source of truth
/// for everything else, and the account keeps a copy of the email address as its sign-in name.
/// </summary>
/// <remarks>Public only because the database context derives from the identity context of this type.</remarks>
public sealed class UserAccount : IdentityUser<Guid>;
