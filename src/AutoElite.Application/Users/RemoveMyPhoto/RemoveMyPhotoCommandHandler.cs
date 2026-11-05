namespace AutoElite.Application.Users.RemoveMyPhoto;

/// <summary>Clears the photo of the caller.</summary>
internal sealed class RemoveMyPhotoCommandHandler(IUserRepository userRepository, ICurrentUser currentUser)
    : ICommandHandler<RemoveMyPhotoCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveMyPhotoCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId(), cancellationToken);

        return user is null ? Result.Failure(ApplicationErrors.NotFound<User>()) : user.RemovePhoto();
    }
}
