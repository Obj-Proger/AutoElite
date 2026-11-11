using AutoElite.API.Common;
using AutoElite.Application.TestTemplates;
using AutoElite.Application.TestTemplates.CreateTestTemplate;
using AutoElite.Application.TestTemplates.DeactivateTestTemplate;
using AutoElite.Application.TestTemplates.GetTestTemplate;
using AutoElite.Application.TestTemplates.GetTestTemplates;
using AutoElite.Application.TestTemplates.ReactivateTestTemplate;
using AutoElite.Application.TestTemplates.UpdateTestTemplate;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The templates of the theory tests: how many questions from which topics, the time limit and the rules for
/// mistakes. Managed by administrators and theory instructors; students read the active ones to choose a practice
/// test.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/test-templates")]
public sealed class TestTemplatesController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the templates by name; students get only the active ones.</summary>
    /// <param name="category">The licence category of the templates.</param>
    /// <param name="isActive">Whether the templates are active; all when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The templates.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TestTemplateItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(LicenseCategory? category, bool? isActive, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetTestTemplatesQuery(category, isActive), cancellationToken));

    /// <summary>Reads a template with its rules and blocks; students read only active templates.</summary>
    /// <param name="templateId">The template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The template.</returns>
    [HttpGet("{templateId:guid}")]
    [ProducesResponseType<TestTemplateDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTemplate(Guid templateId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetTestTemplateQuery(templateId), cancellationToken));

    /// <summary>
    /// Creates a template. The traffic police exam is 20 questions in 20 minutes with 2 mistakes allowed, at most 1 per
    /// block, and 5 questions and 5 minutes added for each mistake.
    /// </summary>
    /// <param name="command">The name, the category, the rules and the blocks.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the template.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreateTemplate(CreateTestTemplateCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetTemplate), "templateId");

    /// <summary>Replaces the content of an active template; tests already started keep the rules they started with.</summary>
    /// <param name="templateId">The template; it takes the place of the one in the body.</param>
    /// <param name="command">The name, the category, the rules and the blocks.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{templateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateTemplate(
        Guid templateId, UpdateTestTemplateCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { TemplateId = templateId }, cancellationToken));

    /// <summary>Stops offering a template for new tests; tests already taken keep it.</summary>
    /// <param name="templateId">The template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{templateId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid templateId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateTestTemplateCommand(templateId), cancellationToken));

    /// <summary>Offers a template for new tests again.</summary>
    /// <param name="templateId">The template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{templateId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid templateId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateTestTemplateCommand(templateId), cancellationToken));
}
