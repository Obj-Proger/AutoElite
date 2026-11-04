using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Users.Invitations;

/// <summary>Emails a user the link to set the first password, in the user's language.</summary>
internal sealed class InvitationSender(IIdentityService identityService, IClientLinks clientLinks, IEmailQueue emailQueue)
{
    /// <summary>Generates a new invitation token and queues the invitation email.</summary>
    /// <param name="user">The invited user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task SendAsync(User user, CancellationToken cancellationToken)
    {
        var token = await identityService.GenerateInvitationTokenAsync(user.Id, cancellationToken);
        var link = clientLinks.Invitation(user.Id, token);
        var culture = user.PreferredLanguage.ToCulture();

        var message = new EmailMessage(
            user.Email,
            EmailTexts.Format(EmailTexts.InvitationSubject, culture),
            EmailTexts.Format(EmailTexts.InvitationBody, culture, user.FullName.FirstName, link));

        await emailQueue.EnqueueAsync(message, cancellationToken);
    }
}
