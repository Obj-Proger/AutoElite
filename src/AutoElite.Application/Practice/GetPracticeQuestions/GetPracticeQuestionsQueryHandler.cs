namespace AutoElite.Application.Practice.GetPracticeQuestions;

/// <summary>
/// Lists the questions of the practice, shuffles them by the seed and reads the part asked for; a topic only narrows
/// the practice of a topic.
/// </summary>
internal sealed class GetPracticeQuestionsQueryHandler(
    PracticeContracts contracts, IPracticeQueries practiceQueries, ICurrentUser currentUser)
    : IQueryHandler<GetPracticeQuestionsQuery, PracticeQuestionPage>
{
    /// <inheritdoc />
    public async Task<Result<PracticeQuestionPage>> Handle(GetPracticeQuestionsQuery query, CancellationToken cancellationToken)
    {
        var contract = await contracts.LoadAsync(query.ContractId, cancellationToken);
        if (contract.IsFailure)
            return Result.Failure<PracticeQuestionPage>(contract.Error);

        var questionIds = await practiceQueries.GetQuestionIdsAsync(
            currentUser.GetRequiredUserId(),
            contract.Value.Category,
            query.Mode == PracticeMode.Topic ? query.TopicId : null,
            query.Mode == PracticeMode.Mistakes,
            cancellationToken);
        List<Guid> part = [.. PracticeOrder.Shuffle(questionIds, query.Seed).Skip(query.Offset).Take(query.Count)];
        IReadOnlyList<PracticeQuestion> questions =
            part.Count == 0 ? [] : await practiceQueries.GetQuestionsAsync(part, cancellationToken);

        return new PracticeQuestionPage(questionIds.Count, questions);
    }
}
