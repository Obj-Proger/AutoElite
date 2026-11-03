namespace AutoElite.Domain.Tests.Entities;

public class ChatTests
{
    private static readonly DateTime _now = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _later = _now.AddHours(1);
    private static readonly Guid _anna = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _boris = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _clara = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _denis = new("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _studyGroupId = new("55555555-5555-5555-5555-555555555555");

    private static Chat CreateGroup() => Chat.CreateGroup("B-2026-02", _anna, [_boris, _clara], null, _now).Value;

    private static ChatMember Member(Chat chat, Guid userId) => chat.CurrentMembers.Single(member => member.UserId == userId);

    [Fact]
    public void CreateDirect_TwoUsers_ReturnsChatWithBothMembersAndDirectKey()
    {
        var result = Chat.CreateDirect(_boris, _anna, _now);

        result.IsSuccess.Should().BeTrue();
        var chat = result.Value;
        chat.Kind.Should().Be(ChatKind.Direct);
        chat.Title.Should().BeNull();
        chat.DirectKey.Should().Be(Chat.BuildDirectKey(_anna, _boris));
        chat.CurrentMembers.Select(member => member.UserId).Should().BeEquivalentTo(new[] { _anna, _boris });
        chat.CurrentMembers.Should().OnlyContain(member => member.Role == ChatMemberRole.Member && member.JoinedAtUtc == _now);
    }

    [Fact]
    public void BuildDirectKey_IsTheSameInEitherOrder()
    {
        Chat.BuildDirectKey(_anna, _boris).Should().Be(Chat.BuildDirectKey(_boris, _anna));
    }

    [Fact]
    public void CreateDirect_SameUser_ReturnsDirectChatWithSelfError()
    {
        var result = Chat.CreateDirect(_anna, _anna, _now);

        result.Error.Should().Be(DomainErrors.Chat.DirectChatWithSelf);
    }

    [Fact]
    public void CreateDirect_EmptyUserId_ReturnsUserIdEmptyError()
    {
        var result = Chat.CreateDirect(_anna, Guid.Empty, _now);

        result.Error.Should().Be(DomainErrors.Chat.UserIdEmpty);
    }

    [Fact]
    public void CreateGroup_ValidData_MakesCreatorAdministrator()
    {
        var result = Chat.CreateGroup("  B-2026-02  ", _anna, [_anna, _boris, _clara], _studyGroupId, _now);

        result.IsSuccess.Should().BeTrue();
        var chat = result.Value;
        chat.Kind.Should().Be(ChatKind.Group);
        chat.Title.Should().Be("B-2026-02");
        chat.DirectKey.Should().BeNull();
        chat.StudyGroupId.Should().Be(_studyGroupId);
        chat.CurrentMembers.Should().HaveCount(3);
        Member(chat, _anna).Role.Should().Be(ChatMemberRole.Administrator);
        Member(chat, _boris).Role.Should().Be(ChatMemberRole.Member);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateGroup_EmptyTitle_ReturnsTitleEmptyError(string? title)
    {
        var result = Chat.CreateGroup(title, _anna, [_boris], null, _now);

        result.Error.Should().Be(DomainErrors.Chat.TitleEmpty);
    }

    [Fact]
    public void CreateGroup_RepeatedMember_ReturnsDuplicateMemberError()
    {
        var result = Chat.CreateGroup("Chat", _anna, [_boris, _boris], null, _now);

        result.Error.Should().Be(DomainErrors.Chat.DuplicateMember);
    }

    [Fact]
    public void CreateGroup_EmptyMemberId_ReturnsUserIdEmptyError()
    {
        var result = Chat.CreateGroup("Chat", _anna, [Guid.Empty], null, _now);

        result.Error.Should().Be(DomainErrors.Chat.UserIdEmpty);
    }

    [Fact]
    public void Rename_ByAdministrator_ChangesTitle()
    {
        var chat = CreateGroup();

        var result = chat.Rename(_anna, "Evening group");

        result.IsSuccess.Should().BeTrue();
        chat.Title.Should().Be("Evening group");
    }

    [Fact]
    public void Rename_ByMember_ReturnsNotAdministratorError()
    {
        var chat = CreateGroup();

        var result = chat.Rename(_boris, "Evening group");

        result.Error.Should().Be(DomainErrors.Chat.NotAdministrator);
        chat.Title.Should().Be("B-2026-02");
    }

    [Fact]
    public void Rename_DirectChat_ReturnsNotGroupChatError()
    {
        var chat = Chat.CreateDirect(_anna, _boris, _now).Value;

        var result = chat.Rename(_anna, "Us");

        result.Error.Should().Be(DomainErrors.Chat.NotGroupChat);
    }

    [Fact]
    public void AddMember_ByAdministrator_AddsMemberAndRaisesEvent()
    {
        var chat = CreateGroup();

        var result = chat.AddMember(_anna, _denis, _later);

        result.IsSuccess.Should().BeTrue();
        Member(chat, _denis).JoinedAtUtc.Should().Be(_later);
        chat.DomainEvents.Should().ContainSingle().Which.Should().Be(new ChatMemberAddedDomainEvent(chat.Id, _denis, _later));
    }

    [Fact]
    public void AddMember_BySystem_AddsMember()
    {
        var chat = CreateGroup();

        var result = chat.AddMember(null, _denis, _later);

        result.IsSuccess.Should().BeTrue();
        chat.IsCurrentMember(_denis).Should().BeTrue();
    }

    [Fact]
    public void AddMember_ByMember_ReturnsNotAdministratorError()
    {
        var chat = CreateGroup();

        var result = chat.AddMember(_boris, _denis, _later);

        result.Error.Should().Be(DomainErrors.Chat.NotAdministrator);
    }

    [Fact]
    public void AddMember_CurrentMember_ReturnsAlreadyMemberError()
    {
        var chat = CreateGroup();

        var result = chat.AddMember(_anna, _boris, _later);

        result.Error.Should().Be(DomainErrors.Chat.AlreadyMember);
    }

    [Fact]
    public void AddMember_UserWhoLeft_CreatesNewMembershipAndKeepsHistory()
    {
        var chat = CreateGroup();
        chat.Leave(_boris, _now);

        var result = chat.AddMember(_anna, _boris, _later);

        result.IsSuccess.Should().BeTrue();
        chat.Members.Count(member => member.UserId == _boris).Should().Be(2);
        chat.IsCurrentMember(_boris).Should().BeTrue();
    }

    [Fact]
    public void RemoveMember_ByAdministrator_EndsMembershipAndRaisesEvent()
    {
        var chat = CreateGroup();

        var result = chat.RemoveMember(_anna, _boris, _later);

        result.IsSuccess.Should().BeTrue();
        chat.IsCurrentMember(_boris).Should().BeFalse();
        chat.Members.Single(member => member.UserId == _boris).LeftAtUtc.Should().Be(_later);
        chat.DomainEvents.Should().ContainSingle().Which.Should().Be(new ChatMemberLeftDomainEvent(chat.Id, _boris, _later));
    }

    [Fact]
    public void RemoveMember_Self_ReturnsCannotRemoveSelfError()
    {
        var chat = CreateGroup();

        var result = chat.RemoveMember(_anna, _anna, _later);

        result.Error.Should().Be(DomainErrors.Chat.CannotRemoveSelf);
    }

    [Fact]
    public void RemoveMember_NotAMember_ReturnsNotMemberError()
    {
        var chat = CreateGroup();

        var result = chat.RemoveMember(_anna, _denis, _later);

        result.Error.Should().Be(DomainErrors.Chat.NotMember);
    }

    [Fact]
    public void ChangeRole_PromoteMember_MakesThemAdministrator()
    {
        var chat = CreateGroup();

        var result = chat.ChangeRole(_anna, _boris, ChatMemberRole.Administrator);

        result.IsSuccess.Should().BeTrue();
        Member(chat, _boris).Role.Should().Be(ChatMemberRole.Administrator);
    }

    [Fact]
    public void ChangeRole_DemoteLastAdministrator_ReturnsLastAdministratorError()
    {
        var chat = CreateGroup();

        var result = chat.ChangeRole(_anna, _anna, ChatMemberRole.Member);

        result.Error.Should().Be(DomainErrors.Chat.LastAdministrator);
    }

    [Fact]
    public void ChangeRole_DemoteAdministratorWhileAnotherRemains_IsAllowed()
    {
        var chat = CreateGroup();
        chat.ChangeRole(_anna, _boris, ChatMemberRole.Administrator);

        var result = chat.ChangeRole(_boris, _anna, ChatMemberRole.Member);

        result.IsSuccess.Should().BeTrue();
        Member(chat, _anna).Role.Should().Be(ChatMemberRole.Member);
    }

    [Fact]
    public void ChangeRole_UndefinedRole_ReturnsRoleNotDefinedError()
    {
        var chat = CreateGroup();

        var result = chat.ChangeRole(_anna, _boris, (ChatMemberRole)9);

        result.Error.Should().Be(DomainErrors.Chat.RoleNotDefined);
    }

    [Fact]
    public void Leave_Member_EndsMembership()
    {
        var chat = CreateGroup();

        var result = chat.Leave(_clara, _later);

        result.IsSuccess.Should().BeTrue();
        chat.IsCurrentMember(_clara).Should().BeFalse();
        Member(chat, _anna).Role.Should().Be(ChatMemberRole.Administrator);
    }

    [Fact]
    public void Leave_LastAdministrator_PromotesLongestStandingMember()
    {
        var chat = CreateGroup();
        chat.AddMember(_anna, _denis, _later);

        chat.Leave(_anna, _later);

        Member(chat, _boris).Role.Should().Be(ChatMemberRole.Administrator);
        Member(chat, _denis).Role.Should().Be(ChatMemberRole.Member);
    }

    [Fact]
    public void Leave_DirectChat_ReturnsNotGroupChatError()
    {
        var chat = Chat.CreateDirect(_anna, _boris, _now).Value;

        var result = chat.Leave(_anna, _later);

        result.Error.Should().Be(DomainErrors.Chat.NotGroupChat);
    }

    [Fact]
    public void Leave_NotAMember_ReturnsNotMemberError()
    {
        var result = CreateGroup().Leave(_denis, _later);

        result.Error.Should().Be(DomainErrors.Chat.NotMember);
    }

    [Fact]
    public void SetMuted_TurnOffThenOn_ChangesSetting()
    {
        var chat = CreateGroup();
        chat.SetMuted(_boris, true);

        var result = chat.SetMuted(_boris, false);

        result.IsSuccess.Should().BeTrue();
        Member(chat, _boris).IsMuted.Should().BeFalse();
    }

    [Fact]
    public void SetMuted_AlreadyMuted_ReturnsAlreadyMutedError()
    {
        var chat = CreateGroup();
        chat.SetMuted(_boris, true);

        var result = chat.SetMuted(_boris, true);

        result.Error.Should().Be(DomainErrors.Chat.AlreadyMuted);
    }

    [Fact]
    public void SetMuted_NotMuted_ReturnsNotMutedError()
    {
        var result = CreateGroup().SetMuted(_boris, false);

        result.Error.Should().Be(DomainErrors.Chat.NotMuted);
    }

    [Fact]
    public void MarkRead_LaterTime_MovesReadPosition()
    {
        var chat = CreateGroup();
        chat.MarkRead(_boris, _now);

        var result = chat.MarkRead(_boris, _later);

        result.IsSuccess.Should().BeTrue();
        Member(chat, _boris).LastReadAtUtc.Should().Be(_later);
    }

    [Fact]
    public void MarkRead_EarlierTime_KeepsReadPosition()
    {
        var chat = CreateGroup();
        chat.MarkRead(_boris, _later);

        chat.MarkRead(_boris, _now);

        Member(chat, _boris).LastReadAtUtc.Should().Be(_later);
    }

    [Fact]
    public void MarkRead_NotAMember_ReturnsNotMemberError()
    {
        var result = CreateGroup().MarkRead(_denis, _later);

        result.Error.Should().Be(DomainErrors.Chat.NotMember);
    }
}
