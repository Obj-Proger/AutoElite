using System.Globalization;
using AutoElite.Application.Common.Events;
using Microsoft.Extensions.Logging;

namespace AutoElite.Application.Notifications.Push;

/// <summary>
/// Sends a new notification to the devices of its recipient as a push notification, with its type, its identifier and
/// what it is about as data, so that tapping it opens the right page. It runs from the outbox, after the notification
/// is saved; a failure of the push service is logged and not retried, since the notification stays in the application.
/// The devices whose tokens the push service no longer knows are removed.
/// </summary>
internal sealed partial class SendPushWhenNotificationCreatedHandler(
    INotificationRepository notificationRepository,
    IPushDeviceRepository deviceRepository,
    IPushSender pushSender,
    ILogger<SendPushWhenNotificationCreatedHandler> logger) : IDomainEventHandler<NotificationCreatedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(NotificationCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var devices = await deviceRepository.ListOfUserAsync(domainEvent.RecipientId, cancellationToken);
        if (devices.Count == 0)
            return;

        var notification = await notificationRepository.GetByIdAsync(domainEvent.NotificationId, cancellationToken);
        if (notification is null)
            return;

        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["notificationId"] = notification.Id.ToString("D", CultureInfo.InvariantCulture),
            ["type"] = notification.Type.ToString()
        };
        if (notification.Subject is { } subject)
        {
            data["subjectKind"] = subject.Kind.ToString();
            data["subjectId"] = subject.Id.ToString("D", CultureInfo.InvariantCulture);
        }

        IReadOnlyList<string> unknown;
        try
        {
            unknown = await pushSender.SendAsync(
                [.. devices.Select(device => device.Token)],
                new PushMessage(notification.Title, notification.Body, data),
                cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException
            && !cancellationToken.IsCancellationRequested)
        {
            LogPushFailed(logger, exception, domainEvent.NotificationId);

            return;
        }

        foreach (var device in devices.Where(device => unknown.Contains(device.Token, StringComparer.Ordinal)))
            deviceRepository.Remove(device);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The push notification of notification {NotificationId} was not sent.")]
    private static partial void LogPushFailed(ILogger logger, Exception exception, Guid notificationId);
}
