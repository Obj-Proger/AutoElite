namespace AutoElite.Application.Users.AcceptInvitation;

/// <summary>
/// Sets the password through the identity system. A wrong, expired or already used link, an unknown user and
/// a deactivated user all get the same error, so the link reveals nothing about accounts.
/// </summary>
internal sealed class AcceptInvitationCommandHandler(IUserRepository userRepository, IIdentityService identityService)
    : ICommandHandler<AcceptInvitationCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is not { IsActive: true })
            return Result.Failure(ApplicationErrors.Account.InvitationInvalid);

        if (await identityService.IsActivatedAsync(user.Id, cancellationToken))
            return Result.Failure(ApplicationErrors.Account.AlreadyActivated);

        var accepted = await identityService.AcceptInvitationAsync(user.Id, command.Token!, command.Password!, cancellationToken);

        return accepted ? Result.Success() : Result.Failure(ApplicationErrors.Account.InvitationInvalid);
    }
}
