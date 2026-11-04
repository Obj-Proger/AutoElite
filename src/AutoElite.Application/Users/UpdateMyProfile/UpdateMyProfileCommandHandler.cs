namespace AutoElite.Application.Users.UpdateMyProfile;

/// <summary>Applies the new details to the signed-in user.</summary>
internal sealed class UpdateMyProfileCommandHandler(IUserRepository userRepository, ICurrentUser currentUser)
    : ICommandHandler<UpdateMyProfileCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId(), cancellationToken);
        if (user is null)
            return Result.Failure(ApplicationErrors.NotFound<User>());

        var fullNameResult = FullName.Create(command.LastName, command.FirstName, command.MiddleName);
        if (fullNameResult.IsFailure)
            return Result.Failure(fullNameResult.Error);

        var phoneResult = CreateOptionalPhone(command.Phone);
        if (phoneResult.IsFailure)
            return Result.Failure(phoneResult.Error);

        var profileResult = user.UpdateProfile(fullNameResult.Value, phoneResult.Value);
        if (profileResult.IsFailure)
            return profileResult;

        return user.ChangeLanguage(command.PreferredLanguage);
    }

    private static Result<PhoneNumber?> CreateOptionalPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return Result.Success<PhoneNumber?>(null);

        var phoneResult = PhoneNumber.Create(phone);

        return phoneResult.IsSuccess
            ? Result.Success<PhoneNumber?>(phoneResult.Value)
            : Result.Failure<PhoneNumber?>(phoneResult.Error);
    }
}
