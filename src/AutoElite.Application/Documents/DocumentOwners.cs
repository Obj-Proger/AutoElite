using AutoElite.Application.Chats;
using AutoElite.Application.Contracts;
using AutoElite.Application.Courses;
using AutoElite.Application.Questions;
using AutoElite.Application.StaffTasks;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.TheoryLessons;
using AutoElite.Application.Users;

namespace AutoElite.Application.Documents;

/// <summary>
/// Decides what the caller may do with the documents of a record (M-13): staff work with documents of the records
/// they work with, students with their own.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Owner</term><description>Access</description></listheader>
/// <item><term>Lead</term><description>Administrators and managers of its branch manage.</description></item>
/// <item>
/// <term>Contract</term>
/// <description>Administrators and managers of its branch manage; its student reads.</description>
/// </item>
/// <item>
/// <term>User</term>
/// <description>The user contributes; administrators and managers of the user's branch manage.</description>
/// </item>
/// <item>
/// <term>Vehicle</term>
/// <description>Administrators manage; managers of its branch and driving instructors read.</description>
/// </item>
/// <item>
/// <term>Study group</term>
/// <description>
/// Administrators, managers of its branch and its theory instructor manage; its current students read.
/// </description>
/// </item>
/// <item>
/// <term>Theory lesson</term>
/// <description>As its group, and the instructor giving the lesson manages too.</description>
/// </item>
/// <item>
/// <term>Course topic</term>
/// <description>Administrators manage; theory instructors contribute; everyone signed in reads.</description>
/// </item>
/// <item><term>Chat</term><description>Its current members contribute.</description></item>
/// <item>
/// <term>Question</term>
/// <description>
/// Administrators manage; theory instructors manage the school's own questions; everyone signed in reads, since tests
/// show the illustrations.
/// </description>
/// </item>
/// <item><term>Staff task</term><description>Whoever manages the task manages; its assignee contributes.</description></item>
/// </list>
/// Documents cannot be attached to other kinds of records.
/// </remarks>
internal sealed class DocumentOwners(
    IRepository<Lead> leadRepository,
    IContractRepository contractRepository,
    IUserRepository userRepository,
    IRepository<Vehicle> vehicleRepository,
    IStudyGroupRepository groupRepository,
    ITheoryLessonRepository lessonRepository,
    ICourseRepository courseRepository,
    IChatRepository chatRepository,
    IQuestionRepository questionRepository,
    StaffTaskAccess taskAccess,
    ICurrentUser currentUser)
{
    /// <summary>The kinds of records documents can be attached to.</summary>
    public static IReadOnlySet<EntityKind> SupportedKinds { get; } = new HashSet<EntityKind>
    {
        EntityKind.Lead, EntityKind.Contract, EntityKind.User, EntityKind.Vehicle, EntityKind.StudyGroup,
        EntityKind.TheoryLesson, EntityKind.CourseTopic, EntityKind.Chat, EntityKind.StaffTask, EntityKind.Question
    };

    private DataScope Scope => DataScope.For(currentUser);

    private bool IsAdministrator => currentUser.IsInRole(UserRole.Administrator);

    private bool IsManager => currentUser.IsInRole(UserRole.Manager);

    /// <summary>Finds what the caller may do with the documents of the record.</summary>
    /// <param name="owner">The record.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The access level; <see cref="DocumentAccessLevel.None"/> also for a record that does not exist.</returns>
    public Task<DocumentAccessLevel> GetAccessAsync(EntityReference owner, CancellationToken cancellationToken) =>
        owner.Kind switch
        {
            EntityKind.Lead => ForLeadAsync(owner.Id, cancellationToken),
            EntityKind.Contract => ForContractAsync(owner.Id, cancellationToken),
            EntityKind.User => ForUserAsync(owner.Id, cancellationToken),
            EntityKind.Vehicle => ForVehicleAsync(owner.Id, cancellationToken),
            EntityKind.StudyGroup => ForGroupAsync(owner.Id, cancellationToken),
            EntityKind.TheoryLesson => ForLessonAsync(owner.Id, cancellationToken),
            EntityKind.CourseTopic => ForTopicAsync(owner.Id, cancellationToken),
            EntityKind.Chat => ForChatAsync(owner.Id, cancellationToken),
            EntityKind.StaffTask => ForTaskAsync(owner.Id, cancellationToken),
            EntityKind.Question => ForQuestionAsync(owner.Id, cancellationToken),
            _ => Task.FromResult(DocumentAccessLevel.None)
        };

    private async Task<DocumentAccessLevel> ForLeadAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await leadRepository.GetByIdAsync(leadId, cancellationToken);

        return lead is not null && ManagesBranch(lead.BranchId) ? DocumentAccessLevel.Manage : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForContractAsync(Guid contractId, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(contractId, cancellationToken);
        if (contract is null)
            return DocumentAccessLevel.None;

        if (ManagesBranch(contract.BranchId))
            return DocumentAccessLevel.Manage;

        return contract.StudentId == currentUser.UserId ? DocumentAccessLevel.Read : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return DocumentAccessLevel.None;

        if (ManagesBranch(user.BranchId))
            return DocumentAccessLevel.Manage;

        return user.Id == currentUser.UserId ? DocumentAccessLevel.Contribute : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null)
            return DocumentAccessLevel.None;

        if (IsAdministrator)
            return DocumentAccessLevel.Manage;

        return (IsManager && Scope.Includes(vehicle.BranchId)) || currentUser.IsInRole(UserRole.DrivingInstructor)
            ? DocumentAccessLevel.Read
            : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);

        return group is null ? DocumentAccessLevel.None : ForGroup(group, instructorId: null);
    }

    private async Task<DocumentAccessLevel> ForLessonAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        var group = lesson is null ? null : await groupRepository.GetByIdAsync(lesson.GroupId, cancellationToken);

        return lesson is null || group is null ? DocumentAccessLevel.None : ForGroup(group, lesson.InstructorId);
    }

    private DocumentAccessLevel ForGroup(StudyGroup group, Guid? instructorId)
    {
        if (ManagesBranch(group.BranchId) || group.TheoryInstructorId == currentUser.UserId || instructorId == currentUser.UserId)
            return DocumentAccessLevel.Manage;

        return group.Enrollments.Any(enrollment => enrollment.IsCurrent && enrollment.StudentId == currentUser.UserId)
            ? DocumentAccessLevel.Read
            : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForTopicAsync(Guid topicId, CancellationToken cancellationToken)
    {
        if (await courseRepository.GetByTopicAsync(topicId, cancellationToken) is null)
            return DocumentAccessLevel.None;

        if (IsAdministrator)
            return DocumentAccessLevel.Manage;

        return currentUser.IsInRole(UserRole.TheoryInstructor) ? DocumentAccessLevel.Contribute : DocumentAccessLevel.Read;
    }

    private async Task<DocumentAccessLevel> ForChatAsync(Guid chatId, CancellationToken cancellationToken)
    {
        var chat = await chatRepository.GetByIdAsync(chatId, cancellationToken);

        return chat is not null && currentUser.UserId is { } userId && chat.IsCurrentMember(userId)
            ? DocumentAccessLevel.Contribute
            : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        if ((await taskAccess.LoadForManagementAsync(taskId, cancellationToken)).IsSuccess)
            return DocumentAccessLevel.Manage;

        return (await taskAccess.LoadForWorkAsync(taskId, cancellationToken)).IsSuccess
            ? DocumentAccessLevel.Contribute
            : DocumentAccessLevel.None;
    }

    private async Task<DocumentAccessLevel> ForQuestionAsync(Guid questionId, CancellationToken cancellationToken)
    {
        var question = await questionRepository.GetByIdAsync(questionId, cancellationToken);
        if (question is null)
            return DocumentAccessLevel.None;

        var manages = IsAdministrator
            || (question.Source == QuestionSource.School && currentUser.IsInRole(UserRole.TheoryInstructor));

        return manages ? DocumentAccessLevel.Manage : DocumentAccessLevel.Read;
    }

    private bool ManagesBranch(Guid? branchId) => IsAdministrator || (IsManager && Scope.Includes(branchId));
}
