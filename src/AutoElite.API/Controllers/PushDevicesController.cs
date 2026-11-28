using AutoElite.API.Common;
using AutoElite.Application.Notifications.RegisterPushDevice;
using AutoElite.Application.Notifications.UnregisterPushDevice;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The devices of the signed-in user that receive push notifications: the application registers its token after
/// signing in and whenever the push service changes it, and unregisters it when the user signs out.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/push-devices")]
public sealed class PushDevicesController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Registers the device for the user; a token registered by another user before passes to this one.</summary>
    /// <param name="command">The token and the push service.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Register(RegisterPushDeviceCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>
    /// Stops the push notifications of the user on the device; a token that is not theirs is left as it is, and the
    /// request still succeeds.
    /// </summary>
    /// <param name="command">The token of the device.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("unregister")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unregister(UnregisterPushDeviceCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));
}
