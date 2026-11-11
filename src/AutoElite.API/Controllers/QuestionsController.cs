using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Questions;
using AutoElite.Application.Questions.CreateQuestion;
using AutoElite.Application.Questions.DeactivateQuestion;
using AutoElite.Application.Questions.GetQuestion;
using AutoElite.Application.Questions.GetQuestions;
using AutoElite.Application.Questions.ImportQuestions;
using AutoElite.Application.Questions.ReactivateQuestion;
using AutoElite.Application.Questions.RemoveQuestionImage;
using AutoElite.Application.Questions.SetQuestionImage;
using AutoElite.Application.Questions.UpdateQuestion;
using AutoElite.Infrastructure.Import;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The question bank of the theory tests: official questions, managed by administrators, and the school's own, which
/// theory instructors manage too.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/questions")]
public sealed class QuestionsController(IDispatcher dispatcher) : ApiControllerBase
{
    // The largest file the import accepts and the rest of the form around it.
    private const long MaxImportRequestBytes = ImportFileLimits.MaxFileBytes + 1024 * 1024;

    /// <summary>Finds questions, the latest added first, a page at a time.</summary>
    /// <param name="search">Text in the question or its external code.</param>
    /// <param name="courseId">The course of the topics.</param>
    /// <param name="topicId">The topic.</param>
    /// <param name="category">A licence category the questions apply to.</param>
    /// <param name="source">Official or school questions.</param>
    /// <param name="isActive">Whether the questions are used in new tests; all when not given.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of questions on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of questions.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<QuestionListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions(
        string? search,
        Guid? courseId,
        Guid? topicId,
        LicenseCategory? category,
        QuestionSource? source,
        bool? isActive,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(
            new GetQuestionsQuery(search, courseId, topicId, category, source, isActive, page, pageSize), cancellationToken));

    /// <summary>Reads a question with its answer options and the correct answer.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The question.</returns>
    [HttpGet("{questionId:guid}")]
    [ProducesResponseType<QuestionDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetQuestion(Guid questionId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetQuestionQuery(questionId), cancellationToken));

    /// <summary>
    /// Adds a question with 2 to 6 answer options, exactly one of them correct. Only administrators add official
    /// questions.
    /// </summary>
    /// <param name="command">The text, the topic, the categories, the source and the options.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the question.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateQuestion(CreateQuestionCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetQuestion), "questionId");

    /// <summary>
    /// Imports questions into a topic from a file of questions or a ZIP archive with their images; a question whose
    /// external code is already in the bank is updated. The import is all or nothing: if a row is invalid, nothing is
    /// imported and the errors are listed by row. Only administrators import official questions.
    /// </summary>
    /// <param name="file">The file: JSON, CSV, XLSX or a ZIP archive of them with the images.</param>
    /// <param name="topicId">The topic the questions go to.</param>
    /// <param name="source">Official or school questions.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>How many questions were added and updated and how many images were stored.</returns>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImportRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportRequestBytes)]
    [ProducesResponseType<QuestionImportResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Import(
        IFormFile? file, [FromForm] Guid topicId, [FromForm] QuestionSource source, CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();

        return Respond(await dispatcher.Send(
            new ImportQuestionsCommand(content, file?.FileName, topicId, source), cancellationToken));
    }

    /// <summary>
    /// Replaces the content of an active question; its source and image stay, and tests already taken keep the question
    /// as it was asked.
    /// </summary>
    /// <param name="questionId">The question; it takes the place of the one in the body.</param>
    /// <param name="command">The text, the topic, the categories and the options.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateQuestion(
        Guid questionId, UpdateQuestionCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { QuestionId = questionId }, cancellationToken));

    /// <summary>Takes a question out of new tests; tests already taken keep it.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{questionId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid questionId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateQuestionCommand(questionId), cancellationToken));

    /// <summary>Returns a question to new tests.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{questionId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid questionId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateQuestionCommand(questionId), cancellationToken));

    /// <summary>
    /// Makes a JPEG, PNG or WebP image attached to the question as a document its illustration; attempts already drawn
    /// keep the illustration they showed.
    /// </summary>
    /// <param name="questionId">The question; it takes the place of the one in the body.</param>
    /// <param name="command">The document with the image.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{questionId:guid}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> SetImage(Guid questionId, SetQuestionImageCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { QuestionId = questionId }, cancellationToken));

    /// <summary>Removes the illustration of a question; the document stays among the documents of the question.</summary>
    /// <param name="questionId">The question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("{questionId:guid}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RemoveImage(Guid questionId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new RemoveQuestionImageCommand(questionId), cancellationToken));
}
