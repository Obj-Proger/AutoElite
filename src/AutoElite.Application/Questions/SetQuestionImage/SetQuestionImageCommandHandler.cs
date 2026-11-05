using AutoElite.Application.Documents;

namespace AutoElite.Application.Questions.SetQuestionImage;

/// <summary>Checks that the caller may change the question and that the document is its image, and sets it.</summary>
internal sealed class SetQuestionImageCommandHandler(QuestionAccess questionAccess, IDocumentRepository documentRepository)
    : ICommandHandler<SetQuestionImageCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(SetQuestionImageCommand command, CancellationToken cancellationToken)
    {
        var questionResult = await questionAccess.LoadForChangeAsync(command.QuestionId, cancellationToken);
        if (questionResult.IsFailure)
            return questionResult;

        var question = questionResult.Value;
        var document = await documentRepository.GetByIdAsync(command.DocumentId, cancellationToken);
        if (document is null || document.Owner != EntityReference.Create(EntityKind.Question, question.Id).Value)
            return Result.Failure(ApplicationErrors.NotFound<Document>());

        if (!DocumentImages.IsImage(document))
            return Result.Failure(ApplicationErrors.Document.ImageRequired);

        return question.SetImage(document.Id);
    }
}
