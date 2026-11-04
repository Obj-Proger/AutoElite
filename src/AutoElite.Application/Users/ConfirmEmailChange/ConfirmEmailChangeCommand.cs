namespace AutoElite.Application.Users.ConfirmEmailChange;

/// <summary>
/// Confirms a new email address from the emailed link and makes it the user's address and sign-in name.
/// It is open to anonymous callers, because the link may be opened on a device where the user is not signed in.
/// </summary>
/// <param name="UserId">The identifier of the user, from the link.</param>
/// <param name="NewEmail">The new address, from the link.</param>
/// <param name="Token">The email change token, from the link.</param>
[AllowAnonymousRequest]
public sealed record ConfirmEmailChangeCommand(Guid UserId, string? NewEmail, string? Token) : ICommand;
