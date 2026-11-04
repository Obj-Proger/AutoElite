namespace AutoElite.Application.Users.ConfirmEmailChange;

/// <summary>
/// Changes the address of the user and of the sign-in account together. The address is checked for uniqueness again,
/// because another user may have taken it since the link was sent.
/// </summary>
internal sealed class ConfirmEmailChangeCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    TimeProvider timeProvider) : ICommandHandler<ConfirmEmailChangeCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ConfirmEmailChangeCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        var emailResult = Email.Create(command.NewEmail);
        if (user is not { IsActive: true } || emailResult.IsFailure)
            return Result.Failure(ApplicationErrors.Account.EmailChangeInvalid);

        var newEmail = emailResult.Value;
        if (newEmail != user.Email && await userRepository.EmailExistsAsync(newEmail, cancellationToken))
            return Result.Failure(ApplicationErrors.Account.EmailTaken);

        var changeResult = user.ChangeEmail(newEmail, timeProvider.GetUtcNow().UtcDateTime);
        if (changeResult.IsFailure)
            return Result.Failure(ApplicationErrors.Account.EmailChangeInvalid);

        var changed = await identityService.ChangeEmailAsync(user.Id, newEmail, command.Token!, cancellationToken);

        return changed ? Result.Success() : Result.Failure(ApplicationErrors.Account.EmailChangeInvalid);
    }
}
