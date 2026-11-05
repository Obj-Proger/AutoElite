using AutoElite.Application.Documents;

namespace AutoElite.Application.Users.SetMyPhoto;

/// <summary>Checks that the document is an image of the caller and sets it as the photo.</summary>
internal sealed class SetMyPhotoCommandHandler(
    IUserRepository userRepository,
    IDocumentRepository documentRepository,
    ICurrentUser currentUser) : ICommandHandler<SetMyPhotoCommand>
{
    private static readonly HashSet<string> _imageTypes = ["image/jpeg", "image/png", "image/webp"];

    /// <inheritdoc />
    public async Task<Result> Handle(SetMyPhotoCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId(), cancellationToken);
        if (user is null)
            return Result.Failure(ApplicationErrors.NotFound<User>());

        var document = await documentRepository.GetByIdAsync(command.DocumentId, cancellationToken);
        if (document is null || document.Owner != EntityReference.Create(EntityKind.User, user.Id).Value)
            return Result.Failure(ApplicationErrors.NotFound<Document>());

        if (!_imageTypes.Contains(document.ContentType))
            return Result.Failure(ApplicationErrors.Document.PhotoNotAnImage);

        return user.SetPhoto(document.Id);
    }
}
