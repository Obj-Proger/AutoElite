using System.Globalization;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A personal or group chat with its members (FR-9.1, FR-9.6..FR-9.8). Messages are a separate aggregate,
/// <see cref="ChatMessage"/>, because a chat can hold many of them.
/// </summary>
/// <remarks>
/// <para>
/// A personal chat has exactly two members and a <see cref="DirectKey"/> built from both user identifiers; a unique
/// index on it ensures that writing to a new person reuses the existing chat instead of creating a second one (FR-9.2).
/// Members cannot leave a personal chat.
/// </para>
/// <para>
/// In a group chat administrators manage members, roles and the title. When the last administrator leaves, the
/// longest-standing remaining member becomes administrator. Methods that manage members take the acting user; passing
/// <see langword="null"/> means the system, which keeps the chat of a study group in step with its enrollments.
/// </para>
/// </remarks>
public sealed class Chat : AuditableEntity
{
    /// <summary>The longest title of a group chat.</summary>
    public const int MaxTitleLength = 200;

    private readonly List<ChatMember> _members = [];

    /// <summary>Gets the kind of the chat.</summary>
    public ChatKind Kind { get; private set; }

    /// <summary>Gets the title of a group chat, or <see langword="null"/> for a personal chat.</summary>
    public string? Title { get; private set; }

    /// <summary>Gets the key of a personal chat, unique per pair of users, or <see langword="null"/> for a group chat.</summary>
    public string? DirectKey { get; private set; }

    /// <summary>Gets the identifier of the study group whose chat this is, if any.</summary>
    public Guid? StudyGroupId { get; private set; }

    /// <summary>Gets all memberships, current and ended, in the order they began.</summary>
    public IReadOnlyList<ChatMember> Members => _members.AsReadOnly();

    /// <summary>Gets the current members.</summary>
    public IEnumerable<ChatMember> CurrentMembers => _members.Where(member => member.IsCurrent);

    private Chat(Guid id, ChatKind kind, string? title, string? directKey, Guid? studyGroupId) : base(id)
    {
        Kind = kind;
        Title = title;
        DirectKey = directKey;
        StudyGroupId = studyGroupId;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Chat()
    {
    }

    /// <summary>Builds the key that identifies the personal chat of two users, regardless of their order.</summary>
    public static string BuildDirectKey(Guid firstUserId, Guid secondUserId) =>
        firstUserId.CompareTo(secondUserId) < 0
            ? string.Create(CultureInfo.InvariantCulture, $"{firstUserId:N}:{secondUserId:N}")
            : string.Create(CultureInfo.InvariantCulture, $"{secondUserId:N}:{firstUserId:N}");

    /// <summary>Creates a personal chat between two users.</summary>
    /// <returns>The chat, or a failure if an identifier is empty or both are the same user.</returns>
    public static Result<Chat> CreateDirect(Guid firstUserId, Guid secondUserId, DateTime utcNow)
    {
        if (firstUserId == Guid.Empty || secondUserId == Guid.Empty)
            return Result.Failure<Chat>(DomainErrors.Chat.UserIdEmpty);

        if (firstUserId == secondUserId)
            return Result.Failure<Chat>(DomainErrors.Chat.DirectChatWithSelf);

        var chat = new Chat(Guid.CreateVersion7(), ChatKind.Direct, null, BuildDirectKey(firstUserId, secondUserId), null);
        chat._members.Add(ChatMember.Join(firstUserId, ChatMemberRole.Member, utcNow));
        chat._members.Add(ChatMember.Join(secondUserId, ChatMemberRole.Member, utcNow));

        return chat;
    }

    /// <summary>Creates a group chat; the creator becomes its administrator.</summary>
    /// <param name="title">The title. Required.</param>
    /// <param name="creatorId">The user creating the chat.</param>
    /// <param name="memberIds">The other members, without repetitions; the creator may be omitted.</param>
    /// <param name="studyGroupId">The study group the chat belongs to, if any.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The chat, or the first validation failure encountered.</returns>
    public static Result<Chat> CreateGroup(
        string? title, Guid creatorId, IReadOnlyCollection<Guid> memberIds, Guid? studyGroupId, DateTime utcNow)
    {
        var titleResult = Guard.RequiredText(title, MaxTitleLength, DomainErrors.Chat.TitleEmpty, DomainErrors.Chat.TitleTooLong);
        if (titleResult.IsFailure)
            return Result.Failure<Chat>(titleResult.Error);

        if (creatorId == Guid.Empty || memberIds.Contains(Guid.Empty))
            return Result.Failure<Chat>(DomainErrors.Chat.UserIdEmpty);

        if (memberIds.Distinct().Count() != memberIds.Count)
            return Result.Failure<Chat>(DomainErrors.Chat.DuplicateMember);

        if (studyGroupId is { } groupId && groupId == Guid.Empty)
            return Result.Failure<Chat>(DomainErrors.Chat.StudyGroupIdEmpty);

        var chat = new Chat(Guid.CreateVersion7(), ChatKind.Group, titleResult.Value, null, studyGroupId);
        chat._members.Add(ChatMember.Join(creatorId, ChatMemberRole.Administrator, utcNow));
        chat._members.AddRange(memberIds
            .Where(memberId => memberId != creatorId)
            .Select(memberId => ChatMember.Join(memberId, ChatMemberRole.Member, utcNow)));

        return chat;
    }

    /// <summary>Determines whether the user is currently a member of the chat.</summary>
    public bool IsCurrentMember(Guid userId) => FindCurrent(userId) is not null;

    /// <summary>Renames a group chat.</summary>
    /// <param name="actorId">The administrator renaming the chat, or <see langword="null"/> for the system.</param>
    /// <param name="title">The new title. Required.</param>
    /// <returns>A failure if the chat is personal, the actor is not an administrator, or the title is invalid.</returns>
    public Result Rename(Guid? actorId, string? title)
    {
        var permission = EnsureGroupAdministrator(actorId);
        if (permission.IsFailure)
            return permission;

        var titleResult = Guard.RequiredText(title, MaxTitleLength, DomainErrors.Chat.TitleEmpty, DomainErrors.Chat.TitleTooLong);
        if (titleResult.IsFailure)
            return Result.Failure(titleResult.Error);

        Title = titleResult.Value;

        return Result.Success();
    }

    /// <summary>Adds a member to a group chat and raises <see cref="ChatMemberAddedDomainEvent"/>.</summary>
    /// <param name="actorId">The administrator adding the member, or <see langword="null"/> for the system.</param>
    /// <param name="userId">The user to add; they must not be a current member.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the chat is personal, the actor is not an administrator, or the user is invalid or already a member.</returns>
    public Result AddMember(Guid? actorId, Guid userId, DateTime utcNow)
    {
        var permission = EnsureGroupAdministrator(actorId);
        if (permission.IsFailure)
            return permission;

        if (userId == Guid.Empty)
            return Result.Failure(DomainErrors.Chat.UserIdEmpty);

        if (IsCurrentMember(userId))
            return Result.Failure(DomainErrors.Chat.AlreadyMember);

        _members.Add(ChatMember.Join(userId, ChatMemberRole.Member, utcNow));
        RaiseDomainEvent(new ChatMemberAddedDomainEvent(Id, userId, utcNow));

        return Result.Success();
    }

    /// <summary>Removes another member from a group chat.</summary>
    /// <param name="actorId">The administrator removing the member, or <see langword="null"/> for the system.</param>
    /// <param name="userId">The member to remove; an administrator leaves with <see cref="Leave"/> instead of removing themselves.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the chat is personal, the actor is not an administrator, the user is not a member, or it is the actor.</returns>
    public Result RemoveMember(Guid? actorId, Guid userId, DateTime utcNow)
    {
        var permission = EnsureGroupAdministrator(actorId);
        if (permission.IsFailure)
            return permission;

        if (actorId == userId)
            return Result.Failure(DomainErrors.Chat.CannotRemoveSelf);

        var member = FindCurrent(userId);
        if (member is null)
            return Result.Failure(DomainErrors.Chat.NotMember);

        EndMembership(member, utcNow);

        return Result.Success();
    }

    /// <summary>Changes the role of a member of a group chat.</summary>
    /// <param name="actorId">The administrator making the change, or <see langword="null"/> for the system.</param>
    /// <param name="userId">The member whose role changes.</param>
    /// <param name="role">The new role.</param>
    /// <returns>
    /// A failure if the chat is personal, the actor is not an administrator, the user is not a member, the role is
    /// invalid, or the last administrator would be demoted.
    /// </returns>
    public Result ChangeRole(Guid? actorId, Guid userId, ChatMemberRole role)
    {
        var permission = EnsureGroupAdministrator(actorId);
        if (permission.IsFailure)
            return permission;

        var member = FindCurrent(userId);
        if (member is null)
            return Result.Failure(DomainErrors.Chat.NotMember);

        var roleResult = Guard.DefinedEnum(role, DomainErrors.Chat.RoleNotDefined);
        if (roleResult.IsFailure)
            return Result.Failure(roleResult.Error);

        if (member.Role == ChatMemberRole.Administrator && role == ChatMemberRole.Member && AdministratorCount() == 1)
            return Result.Failure(DomainErrors.Chat.LastAdministrator);

        member.ChangeRole(role);

        return Result.Success();
    }

    /// <summary>
    /// Lets a member leave a group chat. If the last administrator leaves, the longest-standing remaining member becomes
    /// administrator.
    /// </summary>
    /// <returns>A failure if the chat is personal or the user is not a member.</returns>
    public Result Leave(Guid userId, DateTime utcNow)
    {
        if (Kind != ChatKind.Group)
            return Result.Failure(DomainErrors.Chat.NotGroupChat);

        var member = FindCurrent(userId);
        if (member is null)
            return Result.Failure(DomainErrors.Chat.NotMember);

        EndMembership(member, utcNow);

        return Result.Success();
    }

    /// <summary>Turns notifications of the chat off or on for one member (FR-9.6).</summary>
    /// <returns>A failure if the user is not a member or the setting is already as requested.</returns>
    public Result SetMuted(Guid userId, bool isMuted)
    {
        var member = FindCurrent(userId);
        if (member is null)
            return Result.Failure(DomainErrors.Chat.NotMember);

        if (member.IsMuted == isMuted)
            return Result.Failure(isMuted ? DomainErrors.Chat.AlreadyMuted : DomainErrors.Chat.NotMuted);

        member.SetMuted(isMuted);

        return Result.Success();
    }

    /// <summary>
    /// Records that a member has read the chat up to the given time (FR-9.7). An earlier time than the one already
    /// recorded is ignored, so the read position only moves forward.
    /// </summary>
    /// <returns>A failure if the user is not a member.</returns>
    public Result MarkRead(Guid userId, DateTime readUpToUtc)
    {
        var member = FindCurrent(userId);
        if (member is null)
            return Result.Failure(DomainErrors.Chat.NotMember);

        member.MarkReadUpTo(readUpToUtc);

        return Result.Success();
    }

    private ChatMember? FindCurrent(Guid userId) => _members.Find(member => member.IsCurrent && member.UserId == userId);

    private int AdministratorCount() => CurrentMembers.Count(member => member.Role == ChatMemberRole.Administrator);

    private Result EnsureGroupAdministrator(Guid? actorId)
    {
        if (Kind != ChatKind.Group)
            return Result.Failure(DomainErrors.Chat.NotGroupChat);

        if (actorId is { } id && FindCurrent(id) is not { Role: ChatMemberRole.Administrator })
            return Result.Failure(DomainErrors.Chat.NotAdministrator);

        return Result.Success();
    }

    private void EndMembership(ChatMember member, DateTime utcNow)
    {
        var wasLastAdministrator = member.Role == ChatMemberRole.Administrator && AdministratorCount() == 1;

        member.Leave(utcNow);
        RaiseDomainEvent(new ChatMemberLeftDomainEvent(Id, member.UserId, utcNow));

        if (wasLastAdministrator && CurrentMembers.FirstOrDefault() is { } successor)
            successor.ChangeRole(ChatMemberRole.Administrator);
    }
}
