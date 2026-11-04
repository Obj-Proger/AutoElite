namespace AutoElite.Application.TestSessions;

/// <summary>Draws the questions of the blocks of an attempt and of the questions added for mistakes.</summary>
internal sealed class SessionQuestions(IQuestionPicker questionPicker)
{
    /// <summary>Draws the questions of each block, without repeating a question across blocks.</summary>
    /// <param name="template">The template of the attempt.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The questions of each block, or a failure if the bank does not have enough.</returns>
    public async Task<Result<IReadOnlyList<IReadOnlyList<Question>>>> DrawBlocksAsync(
        TestTemplate template, CancellationToken cancellationToken)
    {
        var drawn = new List<IReadOnlyList<Question>>(template.Blocks.Count);
        var shownIds = new List<Guid>();

        foreach (var block in template.Blocks)
        {
            var pick = new QuestionPick(template.Category, template.Source, block.TopicIds, block.QuestionCount, [.. shownIds]);
            var questions = await questionPicker.PickAsync(pick, cancellationToken);
            if (questions.Count < block.QuestionCount)
                return Result.Failure<IReadOnlyList<IReadOnlyList<Question>>>(ApplicationErrors.TestSession.NotEnoughQuestions);

            drawn.Add(questions);
            shownIds.AddRange(questions.Select(question => question.Id));
        }

        return drawn;
    }

    /// <summary>Adds the questions expected after mistakes, drawn from the blocks of the mistakes.</summary>
    /// <param name="session">The attempt.</param>
    /// <param name="source">The source of the template, or <see langword="null"/> for both.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A failure if the bank does not have enough questions or the domain rejects them.</returns>
    public async Task<Result> AddPendingAsync(TestSession session, QuestionSource? source, CancellationToken cancellationToken)
    {
        foreach (var blockNumber in session.PendingAddOnBlocks.ToList())
        {
            var block = session.Blocks[blockNumber - 1];
            var count = session.Rules.ExtraQuestionsPerMistake;
            var shownIds = session.Questions.Select(question => question.QuestionId).ToList();

            var questions = await questionPicker.PickAsync(
                new QuestionPick(session.Category, source, block.TopicIds, count, shownIds), cancellationToken);
            if (questions.Count < count)
                return Result.Failure(ApplicationErrors.TestSession.NotEnoughQuestions);

            var addResult = session.AddOnQuestions(blockNumber, questions);
            if (addResult.IsFailure)
                return addResult;
        }

        return Result.Success();
    }
}
