using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Users.RemoveMyPhoto;

/// <summary>Clears the profile photo of a user whose photo document was deleted, so the photo never points nowhere.</summary>
internal sealed class RemovePhotoWhenDocumentDeletedHandler(IUserRepository userRepository)
    : IDomainEventHandler<DocumentDeletedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(DocumentDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.OwnerKind != EntityKind.User)
            return;

        var user = await userRepository.GetByIdAsync(domainEvent.OwnerId, cancellationToken);
        if (user is { IsActive: true } && user.PhotoDocumentId == domainEvent.DocumentId)
            user.RemovePhoto();
    }
}
