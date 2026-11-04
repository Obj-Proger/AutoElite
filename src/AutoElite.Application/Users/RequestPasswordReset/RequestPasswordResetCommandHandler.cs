using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users.Invitations;

namespace AutoElite.Application.Users.RequestPasswordReset;

/// <summary>Queues the reset email, or the invitation, for an active user with the address.</summary>
internal sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    InvitationSender invitationSender,
    IClientLinks clientLinks,
    IEmailQueue emailQueue) : ICommandHandler<RequestPasswordResetCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        var user = emailResult.IsSuccess
            ? await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken)
            : null;

        if (user is not { IsActive: true })
            return Result.Success();

        if (await identityService.IsActivatedAsync(user.Id, cancellationToken))
            await SendResetLinkAsync(user, cancellationToken);
        else
            await invitationSender.SendAsync(user, cancellationToken);

        return Result.Success();
    }

    private async Task SendResetLinkAsync(User user, CancellationToken cancellationToken)
    {
        var token = await identityService.GeneratePasswordResetTokenAsync(user.Id, cancellationToken);
        var link = clientLinks.PasswordReset(user.Id, token);
        var culture = user.PreferredLanguage.ToCulture();

        var message = new EmailMessage(
            user.Email,
            EmailTexts.Format(EmailTexts.PasswordResetSubject, culture),
            EmailTexts.Format(EmailTexts.PasswordResetBody, culture, user.FullName.FirstName, link));

        await emailQueue.EnqueueAsync(message, cancellationToken);
    }
}
