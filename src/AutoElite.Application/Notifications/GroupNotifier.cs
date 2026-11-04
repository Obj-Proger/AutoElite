using AutoElite.Application.Common.Localization;
using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Notifications;

/// <summary>Notifies the current students of a study group about something that happens at a time.</summary>
internal sealed class GroupNotifier(IStudyGroupRepository groupRepository, LocalTimes localTimes, Notifier notifier)
{
    /// <summary>
    /// Notifies every student currently enrolled in the group; withdrawn students are not notified. Nothing happens
    /// if the group no longer exists.
    /// </summary>
    /// <param name="groupId">The identifier of the group.</param>
    /// <param name="utcTime">The time the notification is about, shown in the local time of the group's branch.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="subject">The record the notification is about.</param>
    /// <param name="textName">The name of the texts, one of the constants of <see cref="NotificationTexts"/>.</param>
    /// <param name="utcNow">The time of the notification, normally when the event occurred.</param>
    /// <param name="arguments">Builds the body values from the group name and the local time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task NotifyStudentsAsync(
        Guid groupId,
        DateTime utcTime,
        NotificationType type,
        EntityReference subject,
        string textName,
        DateTime utcNow,
        Func<string, DateTime, object?[]> arguments,
        CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null)
            return;

        var localTime = await localTimes.ToLocalAsync(utcTime, group.BranchId, cancellationToken);
        var studentIds = group.Enrollments.Where(enrollment => enrollment.IsCurrent).Select(enrollment => enrollment.StudentId);

        await notifier.NotifyAllAsync(
            studentIds, type, subject, textName, utcNow, cancellationToken, arguments(group.Name, localTime));
    }
}
