using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Documents.AttachDocument;

/// <summary>Checks the owner and that content is given; the file rules are the domain's.</summary>
internal sealed class AttachDocumentCommandValidator : AbstractValidator<AttachDocumentCommand>
{
    public AttachDocumentCommandValidator()
    {
        RuleFor(command => command.OwnerKind)
            .IsInEnum()
            .WithError(DomainErrors.EntityReference.KindNotDefined);

        RuleFor(command => command.OwnerId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.EntityReference.IdEmpty);

        RuleFor(command => command.Content)
            .NotNull()
            .WithError(ApplicationErrors.Document.ContentMissing);
    }
}
