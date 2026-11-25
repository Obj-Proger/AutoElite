using AutoElite.API.Common;
using AutoElite.Application.Settings;
using AutoElite.Application.Settings.GetSchoolSettings;
using AutoElite.Application.Settings.GetWebsite;
using AutoElite.Application.Settings.UpdateSchoolSettings;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The settings of the school: read by every signed-in user, changed by administrators; and where the website of the
/// school is.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/settings")]
public sealed class SettingsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Reads the settings.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The settings.</returns>
    [HttpGet]
    [ProducesResponseType<SchoolSettingsDetails>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetSchoolSettingsQuery(), cancellationToken));

    /// <summary>Reads where the website of the school is, for the links of the mobile application to it.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The address of the website.</returns>
    [HttpGet("website")]
    [ProducesResponseType<WebsiteDetails>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebsite(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetWebsiteQuery(), cancellationToken));

    /// <summary>Changes the settings.</summary>
    /// <param name="command">All the settings.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSettings(UpdateSchoolSettingsCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));
}
