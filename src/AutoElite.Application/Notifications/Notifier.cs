using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications;

/// <summary>
/// Creates in-app notifications with the text written in the language of each recipient (D-048). Deactivated users
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
        if (await userRepository.GetByIdAsync(recipientId, cancellationToken) is { } recipient)
            Notify(recipient, type, subject, textName, utcNow, arguments);
    }

    /// <summary>Notifies several users, loading them at once; each gets the text in their own language.</summary>
    /// <param name="recipientIds">The users to notify; repeated identifiers are notified once.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="subject">The record the notification is about, if any, so that the client can open it.</param>
    /// <param name="textName">The name of the texts, one of the constants of <see cref="NotificationTexts"/>.</param>
    /// <param name="utcNow">The time of the notification, normally when the event occurred.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <param name="arguments">The values of the body placeholders, in order.</param>
    public async Task NotifyAllAsync(
        IEnumerable<Guid> recipientIds,
        NotificationType type,
        EntityReference? subject,
        string textName,
        DateTime utcNow,
        CancellationToken cancellationToken,
        params object?[] arguments)
    {
        var ids = recipientIds.ToHashSet();
        if (ids.Count == 0)
            return;

        var recipients = await userRepository.GetByIdsAsync(ids, cancellationToken);
        NotifyAll(recipients, type, subject, textName, utcNow, arguments);
    }

    /// <summary>Notifies users that are already loaded; each gets the text in their own language.</summary>
    /// <param name="recipients">The users to notify.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="subject">The record the notification is about, if any, so that the client can open it.</param>
    /// <param name="textName">The name of the texts, one of the constants of <see cref="NotificationTexts"/>.</param>
    /// <param name="utcNow">The time of the notification, normally when the event occurred.</param>
    /// <param name="arguments">The values of the body placeholders, in order.</param>
    public void NotifyAll(
        IEnumerable<User> recipients,
        NotificationType type,
        EntityReference? subject,
        string textName,
        DateTime utcNow,
        params object?[] arguments)
    {
        foreach (var recipient in recipients.DistinctBy(user => user.Id))
            Notify(recipient, type, subject, textName, utcNow, arguments);
    }

    private void Notify(
        User recipient, NotificationType type, EntityReference? subject, string textName, DateTime utcNow, object?[] arguments)
    {
        if (!recipient.IsActive)
            return;

        var culture = recipient.PreferredLanguage.ToCulture();
        var notificationResult = Notification.Create(
            recipient.Id, type, NotificationTexts.Title(textName, culture), NotificationTexts.Body(textName, culture, arguments),
            subject, utcNow);
        if (notificationResult.IsSuccess)
            notificationRepository.Add(notificationResult.Value);
    }
}
