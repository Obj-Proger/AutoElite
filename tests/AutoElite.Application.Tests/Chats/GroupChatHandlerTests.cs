using AutoElite.Application.Chats;
using AutoElite.Application.Chats.AddChatMember;
using AutoElite.Application.Chats.ChangeChatMemberRole;
using AutoElite.Application.Chats.CreateGroupChat;
using AutoElite.Application.Chats.LeaveChat;
using AutoElite.Application.Chats.RemoveChatMember;
using AutoElite.Application.Chats.RenameChat;
using AutoElite.Application.Chats.StudyGroupChats;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Chats;

public class GroupChatHandlerTests
{
    private readonly FakeChatRepository _chats = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeStudyGroupRepository _groups = new();
    private readonly User _manager = TestData.NewUser("manager@example.com");
    private readonly User _member = TestData.NewUser("member@example.com");
    private readonly User _newcomer = TestData.NewUser("newcomer@example.com");

    public GroupChatHandlerTests()
    {
        _users.Seed(_manager);
        _users.Seed(_member);
        _users.Seed(_newcomer);
    }

    private static FakeCurrentUser SignedIn(User user, UserRole role = UserRole.Student) =>
        new() { UserId = user.Id, Roles = new HashSet<UserRole> { role } };

    private ChatAccess Access(User caller) => new(_chats, SignedIn(caller));

    private Chat SeedChat()
    {
        var chat = Chat.CreateGroup("Managers", _manager.Id, [_member.Id], null, TestData.Now).Value;
        _chats.Seed(chat);

        return chat;
    }

    private Task<Result> Add(Chat chat, User caller, User user) =>
        new AddChatMemberCommandHandler(Access(caller), new ChatUsers(_users), SignedIn(caller), TestData.Clock())
            .Handle(new AddChatMemberCommand(chat.Id, user.Id), TestContext.Current.CancellationToken);

    private static ChatMember? CurrentMember(Chat chat, User user) =>
        chat.CurrentMembers.SingleOrDefault(member => member.UserId == user.Id);

    [Fact]
    public async Task Create_ByStaff_MakesTheCreatorItsAdministrator()
    {
        var result = await new CreateGroupChatCommandHandler(
                _chats, new ChatUsers(_users), SignedIn(_manager, UserRole.Manager), TestData.Clock())
            .Handle(new CreateGroupChatCommand(" Managers ", [_member.Id]), TestContext.Current.CancellationToken);

        var chat = _chats.Items.Should().ContainSingle().Which;
        chat.Id.Should().Be(result.Value);
        chat.Title.Should().Be("Managers");
        CurrentMember(chat, _manager)!.Role.Should().Be(ChatMemberRole.Administrator);
        CurrentMember(chat, _member)!.Role.Should().Be(ChatMemberRole.Member);
    }

    [Fact]
    public async Task Create_WithADeactivatedMember_ReturnsUserNotFound()
    {
        _member.Deactivate(TestData.Now);

        var result = await new CreateGroupChatCommandHandler(
                _chats, new ChatUsers(_users), SignedIn(_manager, UserRole.Manager), TestData.Clock())
            .Handle(new CreateGroupChatCommand("Managers", [_member.Id]), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
        _chats.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Rename_ByAPlainMember_ReturnsNotAdministrator()
    {
        var chat = SeedChat();

        var result = await new RenameChatCommandHandler(Access(_member), SignedIn(_member))
            .Handle(new RenameChatCommand(chat.Id, "Mine"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.Chat.NotAdministrator);
        chat.Title.Should().Be("Managers");
    }

    [Fact]
    public async Task Add_ByTheAdministrator_AddsTheMember()
    {
        var chat = SeedChat();

        var result = await Add(chat, _manager, _newcomer);

        result.IsSuccess.Should().BeTrue();
        CurrentMember(chat, _newcomer).Should().NotBeNull();
    }

    [Fact]
    public async Task Add_ByAStranger_ReturnsChatNotFound()
    {
        var chat = SeedChat();

        var result = await Add(chat, _newcomer, _newcomer);

        result.Error.Should().Be(ApplicationErrors.NotFound<Chat>());
    }

    [Fact]
    public async Task Remove_ByTheAdministrator_RemovesTheMember()
    {
        var chat = SeedChat();

        var result = await new RemoveChatMemberCommandHandler(Access(_manager), SignedIn(_manager), TestData.Clock())
            .Handle(new RemoveChatMemberCommand(chat.Id, _member.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        CurrentMember(chat, _member).Should().BeNull();
    }

    [Fact]
    public async Task ChangeRole_LastAdministratorToMember_ReturnsLastAdministrator()
    {
        var chat = SeedChat();

        var result = await new ChangeChatMemberRoleCommandHandler(Access(_manager), SignedIn(_manager))
            .Handle(new ChangeChatMemberRoleCommand(chat.Id, _manager.Id, ChatMemberRole.Member), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.Chat.LastAdministrator);
    }

    [Fact]
    public async Task Leave_LastAdministrator_HandsTheRoleToTheRemainingMember()
    {
        var chat = SeedChat();

        var result = await new LeaveChatCommandHandler(Access(_manager), SignedIn(_manager), TestData.Clock())
            .Handle(new LeaveChatCommand(chat.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        CurrentMember(chat, _manager).Should().BeNull();
        CurrentMember(chat, _member)!.Role.Should().Be(ChatMemberRole.Administrator);
    }

    [Fact]
    public void ChangeRoleValidator_UndefinedRole_ReportsRoleNotDefined()
    {
        var result = new ChangeChatMemberRoleCommandValidator()
            .Validate(new ChangeChatMemberRoleCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), (ChatMemberRole)99));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(DomainErrors.Chat.RoleNotDefined.Code);
    }

    [Fact]
    public async Task StudentEnrolled_FirstStudent_CreatesTheGroupChatWithTheInstructorAsAdministrator()
    {
        var instructorId = Guid.CreateVersion7();
        var group = SeedStudyGroup(instructorId);

        await Enrolled(group, _member);
        await Enrolled(group, _newcomer);
        await Enrolled(group, _newcomer);

        var chat = _chats.Items.Should().ContainSingle().Which;
        chat.StudyGroupId.Should().Be(group.Id);
        chat.Title.Should().Be(group.Name);
        chat.CurrentMembers.Select(member => member.UserId).Should().BeEquivalentTo<Guid>([instructorId, _member.Id, _newcomer.Id]);
        chat.CurrentMembers.Single(member => member.UserId == instructorId).Role.Should().Be(ChatMemberRole.Administrator);
    }

    [Fact]
    public async Task StudentWithdrawn_LastContractInTheGroup_RemovesTheStudentFromTheChat()
    {
        var group = SeedStudyGroup(Guid.CreateVersion7());
        await Enrolled(group, _member);
        var chat = _chats.Items.Single();

        await new RemoveFromGroupChatWhenStudentWithdrawnHandler(_chats, _groups).Handle(
            new StudentWithdrawnDomainEvent(group.Id, Guid.CreateVersion7(), _member.Id, TestData.Now),
            TestContext.Current.CancellationToken);

        CurrentMember(chat, _member).Should().BeNull();
    }

    private StudyGroup SeedStudyGroup(Guid instructorId)
    {
        var group = StudyGroup.Create(
                "B-2026-03", TestData.NewCourse(), instructorId, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        _groups.Seed(group);

        return group;
    }

    private Task Enrolled(StudyGroup group, User student) =>
        new AddToGroupChatWhenStudentEnrolledHandler(_chats, _groups).Handle(
            new StudentEnrolledDomainEvent(group.Id, Guid.CreateVersion7(), student.Id, TestData.Now),
            TestContext.Current.CancellationToken);
}
