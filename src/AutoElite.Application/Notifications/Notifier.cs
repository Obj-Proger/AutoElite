using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications;

/// <summary>
/// Creates in-app notifications with the text written in the language of the recipient (D-048). Deactivated users
/// get no notifications.
/// </summary>
internal sealed class Notifier(IUserRepository userRepository, IRepository<Notification> notificationRepository)
{
    /// <summary>Notifies a user.</summary>
    /// <param name="recipientId">The user to notify.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="subject">The record the notification is about, if any, so that the client can open it.</param>
    /// <param name="textName">The name of the texts, one of the constants of <see cref="NotificationTexts"/>.</param>
    /// <param name="utcNow">The time of the notification, normally when the event occurred.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <param name="arguments">The values of the body placeholders, in order.</param>
    public async Task NotifyAsync(
        Guid recipientId,
        NotificationType type,
        EntityReference? subject,
        string textName,
        DateTime utcNow,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        var recipient = await userRepository.GetByIdAsync(recipientId, cancellationToken);
        if (recipient is not { IsActive: true })
            return;

        var culture = recipient.PreferredLanguage.ToCulture();
        var notificationResult = Notification.Create(
            recipient.Id, type, NotificationTexts.Title(textName, culture), NotificationTexts.Body(textName, culture, arguments),
            subject, utcNow);
        if (notificationResult.IsSuccess)
            notificationRepository.Add(notificationResult.Value);
    }
}
