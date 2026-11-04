using AutoElite.Application.Users.Invitations;

namespace AutoElite.Application.Users.ResendInvitation;

/// <summary>Checks that the user still needs an invitation and sends it.</summary>
internal sealed class ResendInvitationCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    InvitationSender invitationSender,
    ICurrentUser currentUser) : ICommandHandler<ResendInvitationCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ResendInvitationCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || !DataScope.For(currentUser).Includes(user.BranchId))
            return Result.Failure(ApplicationErrors.NotFound<User>());

        if (!user.IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        if (await identityService.IsActivatedAsync(user.Id, cancellationToken))
            return Result.Failure(ApplicationErrors.Account.AlreadyActivated);

        await invitationSender.SendAsync(user, cancellationToken);

        return Result.Success();
    }
}
