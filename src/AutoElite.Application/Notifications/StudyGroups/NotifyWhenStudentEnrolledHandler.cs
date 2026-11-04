using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Notifications.StudyGroups;

/// <summary>Tells the student that they joined a study group and when its classes start.</summary>
internal sealed class NotifyWhenStudentEnrolledHandler(IStudyGroupRepository groupRepository, Notifier notifier)
    : IDomainEventHandler<StudentEnrolledDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(StudentEnrolledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(domainEvent.GroupId, cancellationToken);
        if (group is null || !group.Enrollments.Any(enrollment => enrollment.IsCurrent && enrollment.ContractId == domainEvent.ContractId))
            return;

        await notifier.NotifyAsync(
            domainEvent.StudentId,
            NotificationType.EnrolledInGroup,
            EntityReference.Create(EntityKind.StudyGroup, group.Id).Value,
            NotificationTexts.EnrolledInGroup,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            group.Name,
            group.StartDate);
    }
}
