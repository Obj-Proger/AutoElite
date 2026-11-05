using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>Checks the file, the topic and the source; the questions are checked row by row by the handler.</summary>
internal sealed class ImportQuestionsCommandValidator : AbstractValidator<ImportQuestionsCommand>
{
    public ImportQuestionsCommandValidator()
    {
        RuleFor(command => command.Content)
            .NotNull()
            .WithError(ApplicationErrors.Question.ImportContentMissing);

        RuleFor(command => command.FileName)
            .NotEmpty()
            .WithError(DomainErrors.Document.FileNameEmpty);

        RuleFor(command => command.TopicId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Question.TopicIdEmpty);

        RuleFor(command => command.Source)
            .IsInEnum()
            .WithError(DomainErrors.Question.SourceNotDefined);
    }
}
