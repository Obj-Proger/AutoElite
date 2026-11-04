using AutoElite.Application.Common.Validation;
using AutoElite.Application.Courses;
using FluentValidation;

namespace AutoElite.Application.TestTemplates;

/// <summary>The validated rules and blocks of a template.</summary>
/// <param name="Rules">The rules.</param>
/// <param name="Blocks">The blocks.</param>
internal sealed record TestTemplateParts(TestRules Rules, IReadOnlyList<TestBlock> Blocks);

/// <summary>Builds and checks the content of a test template for the commands that write it.</summary>
internal sealed class TestTemplateContent(ICourseRepository courseRepository)
{
    /// <summary>
    /// Adds the rules shared by the validators of the commands. The numbers of the rules and the blocks are checked by
    /// the domain, whose error codes name the field.
    /// </summary>
    /// <typeparam name="T">The type of the validated command.</typeparam>
    /// <param name="validator">The validator of the command.</param>
    public static void AddRules<T>(AbstractValidator<T> validator)
        where T : ITestTemplateDefinition
    {
        validator.RuleFor(command => command.Category)
            .IsInEnum()
            .WithError(DomainErrors.TestTemplate.CategoryNotDefined);

        validator.RuleFor(command => command.Source)
            .IsInEnum()
            .WithError(DomainErrors.TestTemplate.SourceNotDefined);

        validator.RuleFor(command => command.Blocks)
            .NotEmpty()
            .WithError(DomainErrors.TestTemplate.NoBlocks);
    }

    /// <summary>Creates the rules and the blocks and checks that every topic of the blocks belongs to a course.</summary>
    /// <param name="definition">The content given by the command.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<TestTemplateParts>> BuildAsync(ITestTemplateDefinition definition, CancellationToken cancellationToken)
    {
        var rulesResult = TestRules.Create(
            definition.QuestionCount, TimeSpan.FromMinutes(definition.TimeLimitMinutes), definition.MaxMistakes,
            definition.MaxMistakesPerBlock, definition.ExtraQuestionsPerMistake, TimeSpan.FromMinutes(definition.ExtraMinutesPerMistake));
        if (rulesResult.IsFailure)
            return Result.Failure<TestTemplateParts>(rulesResult.Error);

        var blocks = new List<TestBlock>();
        foreach (var input in definition.Blocks ?? [])
        {
            var blockResult = TestBlock.Create(input.QuestionCount, input.TopicIds ?? []);
            if (blockResult.IsFailure)
                return Result.Failure<TestTemplateParts>(blockResult.Error);

            blocks.Add(blockResult.Value);
        }

        foreach (var topicId in blocks.SelectMany(block => block.TopicIds).Distinct())
        {
            if (await courseRepository.GetByTopicAsync(topicId, cancellationToken) is null)
                return Result.Failure<TestTemplateParts>(ApplicationErrors.NotFound<CourseTopic>());
        }

        return new TestTemplateParts(rulesResult.Value, blocks);
    }
}
