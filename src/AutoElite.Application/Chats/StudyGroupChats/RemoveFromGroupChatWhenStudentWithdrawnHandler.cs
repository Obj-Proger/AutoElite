using AutoElite.Application.Common.Events;
using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Chats.StudyGroupChats;

/// <summary>
/// Removes a student who leaves a study group from the chat of the group, unless the student is still in the group on
/// another contract. A repeated delivery changes nothing.
/// </summary>
internal sealed class RemoveFromGroupChatWhenStudentWithdrawnHandler(
    IChatRepository chatRepository,
    IStudyGroupRepository groupRepository) : IDomainEventHandler<StudentWithdrawnDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(StudentWithdrawnDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var chat = await chatRepository.GetOfStudyGroupAsync(domainEvent.GroupId, cancellationToken);
        if (chat is null || !chat.IsCurrentMember(domainEvent.StudentId))
            return;

        var group = await groupRepository.GetByIdAsync(domainEvent.GroupId, cancellationToken);
        if (group is not null
            && group.Enrollments.Any(enrollment => enrollment.IsCurrent && enrollment.StudentId == domainEvent.StudentId))
            return;

        // The system acts without an actor, so the domain skips the administrator check.
        chat.RemoveMember(null, domainEvent.StudentId, domainEvent.OccurredOnUtc);
    }
}
