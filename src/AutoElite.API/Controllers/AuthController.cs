using AutoElite.API.Common;
using AutoElite.Application.Users.AcceptInvitation;
using AutoElite.Application.Users.Authentication;
using AutoElite.Application.Users.ConfirmEmailChange;
using AutoElite.Application.Users.Login;
using AutoElite.Application.Users.Logout;
using AutoElite.Application.Users.RefreshTokens;
using AutoElite.Application.Users.RequestPasswordReset;
using AutoElite.Application.Users.ResetPassword;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// Signing in and out, renewing a session, and the links the system emails: accepting an invitation, resetting a
/// password and confirming a new email address (FR-1.1..1.4). Every request here works without a signed-in user.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/auth")]
public sealed class AuthController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Signs in with the email address and the password and starts a session.</summary>
    /// <param name="command">The email address and the password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The tokens of the session.</returns>
    [HttpPost("login")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>
    /// Renews a session: exchanges its refresh token, which is then used up, for new tokens. A refresh token presented
    /// a second time ends all sessions of its user, since it may have been stolen.
    /// </summary>
    /// <param name="command">The refresh token.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The new tokens of the session.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(RefreshTokensCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Ends a session; an unknown or expired refresh token is ignored, so signing out always succeeds.</summary>
    /// <param name="command">The refresh token of the session.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Accepts an invitation from its emailed link: sets the first password and confirms the email address.</summary>
    /// <param name="command">The user and the token from the link, and the password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>
    /// Emails a link to set a new password. It always succeeds, so that it tells nobody which addresses have accounts.
    /// </summary>
    /// <param name="command">The email address of the account.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("password/forgot")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Sets a new password from the emailed link and ends all sessions of the user.</summary>
    /// <param name="command">The user and the token from the link, and the new password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("password/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Confirms a new email address from the emailed link; it becomes the sign-in name.</summary>
    /// <param name="command">The user, the new address and the token from the link.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("email/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailChangeCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));
}
