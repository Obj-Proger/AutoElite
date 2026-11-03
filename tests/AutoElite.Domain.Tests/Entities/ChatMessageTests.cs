namespace AutoElite.Domain.Tests.Entities;

public class ChatMessageTests
{
    private static readonly DateTime _now = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _later = _now.AddMinutes(5);
    private static readonly Guid _anna = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _boris = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _stranger = new("99999999-9999-9999-9999-999999999999");
    private static readonly Guid _documentId = new("66666666-6666-6666-6666-666666666666");

    private static readonly Chat _chat = Chat.CreateDirect(_anna, _boris, _now).Value;

    private static ChatMessage Send(string? text = "Hello") => ChatMessage.Send(_chat, _anna, text, [], _now).Value;

    [Fact]
    public void Send_TextFromMember_ReturnsMessageAndRaisesSentEvent()
    {
        var result = ChatMessage.Send(_chat, _anna, "  Hello  ", [], _now);

        result.IsSuccess.Should().BeTrue();
        var message = result.Value;
        message.ChatId.Should().Be(_chat.Id);
        message.SenderId.Should().Be(_anna);
        message.Text.Should().Be("Hello");
        message.SentAtUtc.Should().Be(_now);
        message.IsDeleted.Should().BeFalse();
        message.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ChatMessageSentDomainEvent(_chat.Id, message.Id, _anna, _now));
    }

    [Fact]
    public void Send_OnlyAttachment_ReturnsMessageWithoutText()
    {
        var result = ChatMessage.Send(_chat, _anna, "   ", [_documentId], _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().BeNull();
        result.Value.AttachmentIds.Should().Equal(_documentId);
    }

    [Fact]
    public void Send_NotAMember_ReturnsSenderNotMemberError()
    {
        var result = ChatMessage.Send(_chat, _stranger, "Hello", [], _now);

        result.Error.Should().Be(DomainErrors.ChatMessage.SenderNotMember);
    }

    [Fact]
    public void Send_NoTextAndNoAttachments_ReturnsEmptyError()
    {
        var result = ChatMessage.Send(_chat, _anna, null, [], _now);

        result.Error.Should().Be(DomainErrors.ChatMessage.Empty);
    }

    [Fact]
    public void Send_TextTooLong_ReturnsTextTooLongError()
    {
        var result = ChatMessage.Send(_chat, _anna, new string('a', 4001), [], _now);

        result.Error.Should().Be(DomainErrors.ChatMessage.TextTooLong);
    }

    [Fact]
    public void Send_ElevenAttachments_ReturnsTooManyAttachmentsError()
    {
        Guid[] attachments = [.. Enumerable.Range(1, 11).Select(index => new Guid(index, 0, 0, new byte[8]))];

        var result = ChatMessage.Send(_chat, _anna, null, attachments, _now);

        result.Error.Should().Be(DomainErrors.ChatMessage.TooManyAttachments);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Send_EmptyOrRepeatedAttachment_ReturnsInvalidAttachmentsError(bool useEmpty)
    {
        Guid[] attachments = useEmpty ? [Guid.Empty] : [_documentId, _documentId];

        var result = ChatMessage.Send(_chat, _anna, null, attachments, _now);

        result.Error.Should().Be(DomainErrors.ChatMessage.InvalidAttachments);
    }

    [Fact]
    public void Delete_BySender_RemovesContentAndRaisesDeletedEvent()
    {
        var message = ChatMessage.Send(_chat, _anna, "Hello", [_documentId], _now).Value;
        message.ClearDomainEvents();

        var result = message.Delete(_anna, _later);

        result.IsSuccess.Should().BeTrue();
        message.IsDeleted.Should().BeTrue();
        message.DeletedAtUtc.Should().Be(_later);
        message.Text.Should().BeNull();
        message.AttachmentIds.Should().BeEmpty();
        message.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ChatMessageDeletedDomainEvent(_chat.Id, message.Id, _later));
    }

    [Fact]
    public void Delete_ByAnotherUser_ReturnsNotSenderError()
    {
        var message = Send();

        var result = message.Delete(_boris, _later);

        result.Error.Should().Be(DomainErrors.ChatMessage.NotSender);
        message.Text.Should().Be("Hello");
    }

    [Fact]
    public void Delete_DeletedMessage_ReturnsAlreadyDeletedError()
    {
        var message = Send();
        message.Delete(_anna, _later);

        var result = message.Delete(_anna, _later);

        result.Error.Should().Be(DomainErrors.ChatMessage.AlreadyDeleted);
    }
}
