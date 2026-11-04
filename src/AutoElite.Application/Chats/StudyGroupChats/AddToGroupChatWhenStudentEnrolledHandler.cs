using AutoElite.Application.Common.Events;
using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Chats.StudyGroupChats;

/// <summary>
/// Adds a student who joins a study group to the chat of the group, creating the chat with the responsible
/// instructor as its administrator for the first student (D-047). A repeated delivery changes nothing.
/// </summary>
internal sealed class AddToGroupChatWhenStudentEnrolledHandler(IChatRepository chatRepository, IStudyGroupRepository groupRepository)
    : IDomainEventHandler<StudentEnrolledDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(StudentEnrolledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var chat = await chatRepository.GetOfStudyGroupAsync(domainEvent.GroupId, cancellationToken);
        if (chat is not null)
        {
            // The system acts without an actor, so the domain skips the administrator check.
            chat.AddMember(null, domainEvent.StudentId, domainEvent.OccurredOnUtc);
            return;
        }

        var group = await groupRepository.GetByIdAsync(domainEvent.GroupId, cancellationToken);
        if (group is null)
            return;

        var chatResult = Chat.CreateGroup(
            group.Name, group.TheoryInstructorId, [domainEvent.StudentId], group.Id, domainEvent.OccurredOnUtc);
        if (chatResult.IsSuccess)
            chatRepository.Add(chatResult.Value);
    }
}
