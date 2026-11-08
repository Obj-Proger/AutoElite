using AutoElite.API.Common;
using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using AutoElite.Application.Users.ChangePassword;
using AutoElite.Application.Users.GetMyProfile;
using AutoElite.Application.Users.RemoveMyPhoto;
using AutoElite.Application.Users.RequestEmailChange;
using AutoElite.Application.Users.SetMyPhoto;
using AutoElite.Application.Users.UpdateMyProfile;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>The signed-in user's own profile, password, email address and photo (FR-1.7).</summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/me")]
public sealed class MeController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Reads the profile of the signed-in user.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The profile.</returns>
    [HttpGet]
    [ProducesResponseType<UserProfile>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyProfileQuery(), cancellationToken));

    /// <summary>Changes the name, the phone and the language of the signed-in user.</summary>
    /// <param name="command">The name, the phone and the language.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile(UpdateMyProfileCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>
    /// Changes the password. All sessions end, on every device, and the caller gets the tokens of a new session.
    /// </summary>
    /// <param name="command">The current and the new password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The tokens of the new session.</returns>
    [HttpPost("password")]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>
    /// Starts changing the email address: a confirmation link is sent to the new address, and the current one stays
    /// the sign-in name until the link is followed.
    /// </summary>
    /// <param name="command">The new address and the current password.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RequestEmailChange(RequestEmailChangeCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Makes one of the user's documents, a JPEG, PNG or WebP image, the profile photo.</summary>
    /// <param name="command">The document with the photo.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> SetPhoto(SetMyPhotoCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Removes the profile photo; the document stays among the user's documents.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePhoto(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new RemoveMyPhotoCommand(), cancellationToken));
}
