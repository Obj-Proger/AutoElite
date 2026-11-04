using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Users.Invitations;

/// <summary>
/// Sends the invitation once a new user is saved. Accounts are created only by staff, so every new user is invited.
/// </summary>
internal sealed class SendInvitationWhenUserRegisteredHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    InvitationSender invitationSender) : IDomainEventHandler<UserRegisteredDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);

        // A user deactivated in the meantime, or one who has already set a password, needs no invitation.
        if (user is not { IsActive: true } || await identityService.IsActivatedAsync(user.Id, cancellationToken))
            return;

        await invitationSender.SendAsync(user, cancellationToken);
    }
}
