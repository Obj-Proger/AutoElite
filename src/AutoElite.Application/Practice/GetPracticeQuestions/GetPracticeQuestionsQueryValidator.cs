using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Practice.GetPracticeQuestions;

/// <summary>Checks the mode, the topic it needs, and the part asked for.</summary>
internal sealed class GetPracticeQuestionsQueryValidator : AbstractValidator<GetPracticeQuestionsQuery>
{
    /// <summary>The most questions read at a time.</summary>
    public const int MaxCount = 50;

    public GetPracticeQuestionsQueryValidator()
    {
        RuleFor(query => query.Mode)
            .IsInEnum()
            .WithError(ApplicationErrors.Practice.ModeNotDefined);

        RuleFor(query => query.TopicId)
            .NotNull()
            .NotEqual(Guid.Empty)
            .When(query => query.Mode == PracticeMode.Topic)
            .WithError(ApplicationErrors.Practice.TopicRequired);

        RuleFor(query => query.Offset)
            .GreaterThanOrEqualTo(0)
            .WithError(ApplicationErrors.Practice.OffsetNegative);

        RuleFor(query => query.Count)
            .InclusiveBetween(1, MaxCount)
            .WithError(ApplicationErrors.Practice.CountOutOfRange);
    }
}
