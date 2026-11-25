using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The practice of a student on the questions of the licence category of their contract, outside tests: how far they
/// have gone, read from the API or, without a network, from the device; and the questions of a practice, read in
/// parts, with each answer checked by the API at once. The API keeps the answers, so the practice needs the network.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class QuestionPractice(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads how far the student has gone through the questions, in all and by topic.</summary>
    /// <param name="contractId">The contract of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The overview.</returns>
    public Task<Cached<PracticeOverview>> ReadOverviewAsync(Guid contractId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"practice/{contractId:N}"),
            token => createApi().Api.V1.Practice.GetAsync(request => request.QueryParameters.ContractId = contractId, token),
            PracticeOverview.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads a part of the questions of a practice, in the order the seed gives.</summary>
    /// <param name="contractId">The contract of the student.</param>
    /// <param name="mode">Which questions are practised.</param>
    /// <param name="topicId">The topic, for the practice of a topic.</param>
    /// <param name="seed">The number that shuffles the questions, the same for every part of the practice.</param>
    /// <param name="offset">How many questions come before the part.</param>
    /// <param name="count">How many questions the part has.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The part, with the number of questions of the practice.</returns>
    public async Task<PracticeQuestionPage> ReadQuestionsAsync(
        Guid contractId, PracticeMode mode, Guid? topicId, int seed, int offset, int count, CancellationToken cancellationToken) =>
        await createApi().Api.V1.Practice.Questions.GetAsync(
            request =>
            {
                request.QueryParameters.ContractId = contractId;
                request.QueryParameters.Mode = mode;
                request.QueryParameters.TopicId = topicId;
                request.QueryParameters.Seed = seed;
                request.QueryParameters.Offset = offset;
                request.QueryParameters.Count = count;
            },
            cancellationToken)
        ?? throw new InvalidOperationException("The API answered without the questions.");

    /// <summary>Checks an answer at once; the API records it among the answers and mistakes of the student.</summary>
    /// <param name="contractId">The contract of the student.</param>
    /// <param name="questionId">The question.</param>
    /// <param name="optionId">The chosen option.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>Whether the answer was right, with the correct option and the explanation.</returns>
    public async Task<PracticeAnswerResult> AnswerAsync(
        Guid contractId, Guid questionId, Guid optionId, CancellationToken cancellationToken) =>
        await createApi().Api.V1.Practice.Answers.PostAsync(
            new AnswerPracticeQuestionCommand { ContractId = contractId, QuestionId = questionId, OptionId = optionId },
            cancellationToken: cancellationToken)
        ?? throw new InvalidOperationException("The API answered without the check of the answer.");
}
