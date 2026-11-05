using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Questions.RemoveQuestionImage;

/// <summary>Clears the illustration of a question whose image document was deleted, so it never points nowhere.</summary>
internal sealed class RemoveImageWhenDocumentDeletedHandler(IQuestionRepository questionRepository)
    : IDomainEventHandler<DocumentDeletedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(DocumentDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.OwnerKind != EntityKind.Question)
            return;

        var question = await questionRepository.GetByIdAsync(domainEvent.OwnerId, cancellationToken);
        if (question is not null && question.ImageDocumentId == domainEvent.DocumentId)
            question.RemoveImage();
    }
}
