using AutoElite.Application.Courses;

namespace AutoElite.Application.Questions;

/// <summary>The checks shared by the commands that write the content of a question.</summary>
internal sealed class QuestionContent(IQuestionRepository questionRepository, ICourseRepository courseRepository)
{
    /// <summary>Checks that the topic belongs to a course.</summary>
    /// <param name="topicId">The identifier of the topic.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckTopicAsync(Guid topicId, CancellationToken cancellationToken) =>
        await courseRepository.GetByTopicAsync(topicId, cancellationToken) is null
            ? Result.Failure(ApplicationErrors.NotFound<CourseTopic>())
            : Result.Success();

    /// <summary>Checks that no other question has the external code of the question.</summary>
    /// <param name="question">The question with its new content.</param>
    /// <param name="isNew">Whether the question is being created, so that there is no other one to leave out.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckExternalCodeAsync(Question question, bool isNew, CancellationToken cancellationToken)
    {
        if (question.ExternalCode is not { } code)
            return Result.Success();

        return await questionRepository.ExternalCodeExistsAsync(code, isNew ? null : question.Id, cancellationToken)
            ? Result.Failure(ApplicationErrors.Question.ExternalCodeTaken)
            : Result.Success();
    }

    /// <summary>Converts the options of a command to the drafts the domain checks.</summary>
    /// <param name="options">The options, or <see langword="null"/> for none.</param>
    public static IReadOnlyList<AnswerOptionDraft> ToDrafts(IReadOnlyList<AnswerOptionInput>? options) =>
        [.. (options ?? []).Select(option => new AnswerOptionDraft(option.Text, option.IsCorrect))];
}
