using System.Net;
using AutoElite.Domain.Enums;

namespace AutoElite.API.Tests.Controllers;

/// <summary>Group and personal chats between users of the school.</summary>
[Collection(nameof(ApiCollection))]
public sealed class ChatsControllerTests(ApiFactory api)
{
    [Fact]
    public async Task Instructor_LeadsAGroupChat_WhoseMembersReadAndAnswer()
    {
        using var instructor = await api.SignedInClientAsync(UserRole.TheoryInstructor);
        var (student, studentUser) = await api.SignInAsync(UserRole.Student);
        using var studentClient = student;
        var (colleague, colleagueUser) = await api.SignInAsync(UserRole.TheoryInstructor);
        using var colleagueClient = colleague;
        var late = await api.InviteAsync(UserRole.Student);

        var group = new { Title = "Группа Б-12", MemberIds = new[] { studentUser.UserId, colleagueUser.UserId } };
        using var created = await instructor.SendJsonAsync(HttpMethod.Post, "/api/v1/chats", group);
        var chatId = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();
        var chat = $"/api/v1/chats/{chatId}";
        var messageId = await instructor.CreateAsync($"{chat}/messages", new { Text = "Завтра занятие в 18:00" });
        var unread = await student.GetJsonAsync("/api/v1/chats");
        var messages = await student.GetJsonAsync($"{chat}/messages?limit=10");
        using var read = await student.SendJsonAsync(HttpMethod.Put, $"{chat}/read-position", new { ReadUpToUtc = DateTime.UtcNow });
        var replyId = await student.CreateAsync($"{chat}/messages", new { Text = "Буду" });
        var afterReading = await student.GetJsonAsync("/api/v1/chats");

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location!.AbsolutePath.Should().Be($"{chat}/messages");
        var listed = unread.EnumerateArray()
            .Should().ContainSingle(item => item.GetProperty("id").GetGuid() == chatId).Which;
        listed.GetProperty("kind").GetString().Should().Be(nameof(ChatKind.Group));
        listed.GetProperty("unreadCount").GetInt32().Should().Be(1);
        messages.EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("text").GetString().Should().Be("Завтра занятие в 18:00");
        read.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterReading.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == chatId)
            .GetProperty("unreadCount").GetInt32().Should().Be(0);

        using var renamed = await instructor.SendJsonAsync(HttpMethod.Put, $"{chat}/title", new { Title = "Группа Б-12, вечер" });
        using var added = await instructor.SendJsonAsync(HttpMethod.Post, $"{chat}/members", new { UserId = late.UserId });
        using var promoted = await instructor.SendJsonAsync(
            HttpMethod.Put, $"{chat}/members/{colleagueUser.UserId}/role", new { Role = ChatMemberRole.Administrator });
        using var removed = await colleague.SendJsonAsync(HttpMethod.Delete, $"{chat}/members/{late.UserId}");
        using var left = await instructor.SendJsonAsync(HttpMethod.Post, $"{chat}/leave");
        using var deleted = await student.SendJsonAsync(HttpMethod.Delete, $"/api/v1/chats/messages/{replyId}");
        var members = await colleague.GetJsonAsync($"{chat}/members");
        var history = await colleague.GetJsonAsync($"{chat}/messages");

        renamed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        added.StatusCode.Should().Be(HttpStatusCode.NoContent);
        promoted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        left.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        members.EnumerateArray().Select(member => member.GetProperty("userId").GetGuid())
            .Should().BeEquivalentTo(new[] { studentUser.UserId, colleagueUser.UserId });
        var reply = history.EnumerateArray().Single(message => message.GetProperty("id").GetGuid() == replyId);
        reply.GetProperty("isDeleted").GetBoolean().Should().BeTrue();
        history.EnumerateArray().Select(message => message.GetProperty("id").GetGuid()).Should().Contain(messageId);
    }

    [Fact]
    public async Task FirstPersonalMessage_OpensTheChatOfThePair()
    {
        var (sender, senderUser) = await api.SignInAsync(UserRole.Student);
        using var senderClient = sender;
        var (recipient, recipientUser) = await api.SignInAsync(UserRole.DrivingInstructor);
        using var recipientClient = recipient;

        var message = new { RecipientId = recipientUser.UserId, Text = "Можно перенести занятие?" };
        using var sent = await sender.SendJsonAsync(HttpMethod.Post, "/api/v1/chats/direct/messages", message);
        using var opened = await recipient.SendJsonAsync(HttpMethod.Post, "/api/v1/chats/direct", new { UserId = senderUser.UserId });
        var chats = await recipient.GetJsonAsync("/api/v1/chats");

        sent.StatusCode.Should().Be(HttpStatusCode.Created);
        opened.StatusCode.Should().Be(HttpStatusCode.OK);
        var chatId = (await opened.ReadJsonAsync()).GetProperty("chatId").GetGuid();
        var chat = chats.EnumerateArray().Should().ContainSingle().Which;
        chat.GetProperty("id").GetGuid().Should().Be(chatId);
        chat.GetProperty("kind").GetString().Should().Be(nameof(ChatKind.Direct));
        chat.GetProperty("counterpartId").GetGuid().Should().Be(senderUser.UserId);
        chat.GetProperty("lastMessageText").GetString().Should().Be("Можно перенести занятие?");
    }
}
