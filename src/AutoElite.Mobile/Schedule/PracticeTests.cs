using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The practice tests of a student: the active templates of the category of their contract, their attempts, and taking
/// an attempt question by question. A test needs the API, which keeps the timer and checks the answers, so it is not
/// taken offline.
/// </summary>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class PracticeTests(Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads the active templates of a licence category, by name.</summary>
    /// <param name="category">The category of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The templates.</returns>
    public async Task<IReadOnlyList<TestTemplateItem>> ReadTemplatesAsync(LicenseCategory category, CancellationToken cancellationToken)
    {
        // A student gets only the active templates; the category is chosen here rather than in the query.
        var templates = await createApi().Api.V1.TestTemplates.GetAsync(cancellationToken: cancellationToken) ?? [];

        return [.. templates.Where(template => template.Category == category && template.IsActive != false)];
    }

    /// <summary>Reads the attempts of a contract, the latest first.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The attempts.</returns>
    public async Task<IReadOnlyList<TestSessionItem>> ReadAttemptsAsync(Guid contractId, CancellationToken cancellationToken) =>
        await createApi().Api.V1.Contracts[contractId].TestSessions.GetAsync(cancellationToken: cancellationToken) ?? [];

    /// <summary>Starts an attempt; the questions are drawn and the timer starts.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="templateId">The template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The attempt.</returns>
    public async Task<Guid> StartAsync(Guid contractId, Guid templateId, CancellationToken cancellationToken)
    {
        var created = await createApi().Api.V1.TestSessions.PostAsync(
            new StartTestCommand { ContractId = contractId, TemplateId = templateId }, cancellationToken: cancellationToken);

        return created?.Id ?? throw new InvalidOperationException("The API started the attempt without giving it.");
    }

    /// <summary>Reads an attempt with its questions; after it, with the correct answers and the explanations.</summary>
    /// <param name="sessionId">The attempt.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The attempt.</returns>
    public async Task<TestSessionDetails> ReadAttemptAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await createApi().Api.V1.TestSessions[sessionId].GetAsync(cancellationToken: cancellationToken)
        ?? throw new InvalidOperationException("The API gave the attempt without content.");

    /// <summary>
    /// Answers a question; an answer after the deadline closes the attempt instead, which is how an attempt whose time
    /// ran out is closed.
    /// </summary>
    /// <param name="sessionId">The attempt.</param>
    /// <param name="questionId">The question of the attempt.</param>
    /// <param name="optionId">The chosen option.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The state of the attempt after the answer.</returns>
    public async Task<TestSessionStatus?> AnswerAsync(
        Guid sessionId, Guid questionId, Guid optionId, CancellationToken cancellationToken)
    {
        var outcome = await createApi().Api.V1.TestSessions[sessionId].Answers.PostAsync(
            new AnswerTestQuestionCommand { SessionId = sessionId, SessionQuestionId = questionId, OptionId = optionId },
            cancellationToken: cancellationToken);

        return outcome?.Status;
    }

    /// <summary>Reads the illustration of a question.</summary>
    /// <param name="documentId">The document of the illustration.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The content of the image.</returns>
    public async Task<byte[]> ReadImageAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var content = await createApi().Api.V1.Documents[documentId].GetAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API gave the image without content.");
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        return buffer.ToArray();
    }
}
