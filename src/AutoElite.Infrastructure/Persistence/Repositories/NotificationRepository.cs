using AutoElite.Application.Notifications;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>
/// Loads and adds notifications, and marks them read in bulk with one UPDATE each, which neither loads nor tracks
/// them: a stale copy of a notification is harmless, since reading it again changes nothing.
/// </summary>
internal sealed class NotificationRepository(AutoEliteDbContext dbContext)
    : Repository<Notification>(dbContext), INotificationRepository
{
    /// <inheritdoc />
    public Task<int> MarkAllAsReadAsync(Guid recipientId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Aggregates
            .Where(notification => notification.RecipientId == recipientId && notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(notification => notification.ReadAtUtc, (DateTime?)utcNow), cancellationToken);

    /// <inheritdoc />
    public Task<int> MarkReadAboutAsync(
        Guid recipientId,
        EntityReference subject,
        DateTime createdUpToUtc,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var (kind, id) = (subject.Kind, subject.Id);

        return Aggregates
            .Where(notification => notification.RecipientId == recipientId
                && notification.ReadAtUtc == null
                && notification.CreatedAtUtc <= createdUpToUtc
                && notification.Subject!.Id == id
                && notification.Subject.Kind == kind)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(notification => notification.ReadAtUtc, (DateTime?)utcNow), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SentNotification>> GetSentSinceAsync(
        NotificationType type,
        IReadOnlyCollection<EntityReference> subjects,
        DateTime createdSinceUtc,
        CancellationToken cancellationToken = default)
    {
        if (subjects.Count == 0)
            return [];

        // Filter by identifier in SQL and by the whole reference here, since a set of pairs is no SQL parameter.
        List<Guid> subjectIds = [.. subjects.Select(subject => subject.Id).Distinct()];
        var found = await Aggregates.AsNoTracking()
            .Where(notification => notification.Type == type
                && notification.CreatedAtUtc >= createdSinceUtc
                && subjectIds.Contains(notification.Subject!.Id))
            .Select(notification => new { notification.RecipientId, notification.Subject, notification.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        var wanted = subjects.ToHashSet();

        return
        [
            .. found
                .Where(notification => notification.Subject is not null && wanted.Contains(notification.Subject))
                .Select(notification => new SentNotification(
                    notification.RecipientId, notification.Subject!, notification.CreatedAtUtc))
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetRecipientsWithUnreadAsync(
        IReadOnlyCollection<Guid> recipientIds,
        NotificationType type,
        EntityReference subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (recipientIds.Count == 0)
            return new HashSet<Guid>();

        List<Guid> ids = [.. recipientIds];
        var (kind, id) = (subject.Kind, subject.Id);
        var found = await Aggregates.AsNoTracking()
            .Where(notification => ids.Contains(notification.RecipientId)
                && notification.Type == type
                && notification.ReadAtUtc == null
                && notification.Subject!.Id == id
                && notification.Subject.Kind == kind)
            .Select(notification => notification.RecipientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return found.ToHashSet();
    }
}
