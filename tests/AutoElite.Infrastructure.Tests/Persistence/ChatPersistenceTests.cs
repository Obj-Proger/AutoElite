using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class ChatPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static ChatMessage Send(Chat chat, User sender, string? text, int minutes, params Guid[] attachmentIds) =>
        ChatMessage.Send(chat, sender.Id, text, attachmentIds, _now.AddMinutes(minutes)).Value;

    [Fact]
    public async Task Save_Message_StoresTheTextEncryptedAndReadsItBack()
    {
        var anna = TrainingRecords.Person("Смирнова", "Анна");
        var boris = TrainingRecords.Person("Белов", "Борис");
        var chat = Chat.CreateDirect(anna.Id, boris.Id, _now).Value;
        var attachmentId = Guid.CreateVersion7();
        var message = Send(chat, anna, "Пароль от площадки: 1234", 1, attachmentId);
        await SaveAsync(anna, boris, chat, message);

        await using var context = database.CreateContext();
        var stored = await context.Database
            .SqlQuery<string>($"SELECT text AS \"Value\" FROM chat_messages WHERE id = {message.Id}")
            .SingleAsync(CancellationToken);
        var loaded = await new ChatMessageRepository(context).GetByIdAsync(message.Id, CancellationToken);
        var repository = new ChatRepository(context);

        stored.Should().StartWith(TestChatEncryption.KeyId + ":").And.NotContain("1234");
        loaded!.Text.Should().Be("Пароль от площадки: 1234");
        loaded.AttachmentIds.Should().Equal(attachmentId);
        (await repository.GetDirectAsync(Chat.BuildDirectKey(boris.Id, anna.Id), CancellationToken))!.Id.Should().Be(chat.Id);
        (await repository.IsAnyAttachedAsync([Guid.CreateVersion7(), attachmentId], CancellationToken)).Should().BeTrue();
        (await repository.IsAnyAttachedAsync([Guid.CreateVersion7()], CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Save_DeletedMessage_KeepsNeitherTextNorAttachments()
    {
        var anna = TrainingRecords.Person("Смирнова", "Анна");
        var boris = TrainingRecords.Person("Белов", "Борис");
        var chat = Chat.CreateDirect(anna.Id, boris.Id, _now).Value;
        var attachmentId = Guid.CreateVersion7();
        var message = Send(chat, anna, "Ошибся чатом", 1, attachmentId);
        await SaveAsync(anna, boris, chat, message);

        await using (var context = database.CreateContext())
        {
            var tracked = await new ChatMessageRepository(context).GetByIdAsync(message.Id, CancellationToken);
            tracked!.Delete(anna.Id, _now.AddMinutes(2)).IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new ChatMessageRepository(reader).GetByIdAsync(message.Id, CancellationToken);
        loaded!.IsDeleted.Should().BeTrue();
        loaded.Text.Should().BeNull();
        loaded.AttachmentIds.Should().BeEmpty();
        (await new ChatRepository(reader).IsAnyAttachedAsync([attachmentId], CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Save_SecondDirectChatOfAPair_ThrowsConcurrencyConflict()
    {
        var anna = TrainingRecords.Person("Смирнова", "Анна");
        var boris = TrainingRecords.Person("Белов", "Борис");
        await SaveAsync(anna, boris, Chat.CreateDirect(anna.Id, boris.Id, _now).Value);

        var save = () => SaveAsync(Chat.CreateDirect(boris.Id, anna.Id, _now).Value);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Save_GroupChatChanges_StoresTheMembershipsAsHistory()
    {
        var admin = TrainingRecords.Person("Орлов");
        var member = TrainingRecords.Person("Андреев");
        var chat = Chat.CreateGroup("Группа B-12", admin.Id, [member.Id], null, _now).Value;
        await SaveAsync(admin, member, chat);

        await using (var context = database.CreateContext())
        {
            var tracked = await new ChatRepository(context).GetByIdAsync(chat.Id, CancellationToken);
            tracked!.RemoveMember(admin.Id, member.Id, _now.AddDays(1)).IsSuccess.Should().BeTrue();
            tracked.AddMember(admin.Id, member.Id, _now.AddDays(2)).IsSuccess.Should().BeTrue();
            tracked.SetMuted(member.Id, true).IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new ChatRepository(reader).GetByIdAsync(chat.Id, CancellationToken);
        loaded!.Members.Should().HaveCount(3);
        loaded.Members.Where(membership => membership.UserId == member.Id).Select(membership => membership.LeftAtUtc)
            .Should().Equal(new DateTime?[] { _now.AddDays(1), null });
        loaded.CurrentMembers.Should().HaveCount(2);
        loaded.CurrentMembers.Single(membership => membership.UserId == member.Id).IsMuted.Should().BeTrue();
    }

    [Fact]
    public async Task Queries_ReadTheChatsOfAUserTheMessagesAndTheMembers()
    {
        var anna = TrainingRecords.Person("Смирнова", "Анна", "Сергеевна");
        var boris = TrainingRecords.Person("Белов", "Борис", null);
        var vera = TrainingRecords.Person("Волкова", "Вера", null);
        var direct = Chat.CreateDirect(anna.Id, boris.Id, _now).Value;
        var group = Chat.CreateGroup("Группа B-12", vera.Id, [boris.Id, anna.Id], null, _now).Value;
        var first = Send(direct, anna, "Привет", 1);
        var second = Send(direct, boris, "Здравствуйте", 2);
        var third = Send(direct, boris, null, 3, Guid.CreateVersion7());
        third.Delete(boris.Id, _now.AddMinutes(4)).IsSuccess.Should().BeTrue();
        var fourth = Send(direct, boris, "Во сколько занятие?", 5);
        await SaveAsync(anna, boris, vera, direct, group, first, second, third, fourth);
        await using (var context = database.CreateContext())
        {
            var tracked = await new ChatRepository(context).GetByIdAsync(direct.Id, CancellationToken);
            tracked!.MarkRead(anna.Id, _now.AddMinutes(2)).IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var queries = new ChatQueries(reader);

        var chats = await queries.GetOfUserAsync(anna.Id, CancellationToken);
        var latest = await queries.GetMessagesAsync(direct.Id, null, 2, CancellationToken);
        var earlier = await queries.GetMessagesAsync(direct.Id, latest[^1].Id, 10, CancellationToken);
        var members = await queries.GetMembersAsync(group.Id, CancellationToken);

        chats.Select(chat => chat.Id).Should().Equal(direct.Id, group.Id);
        var directItem = chats[0];
        directItem.Title.Should().Be("Белов Борис");
        directItem.CounterpartId.Should().Be(boris.Id);
        directItem.LastMessageText.Should().Be("Во сколько занятие?");
        directItem.LastMessageSenderId.Should().Be(boris.Id);
        directItem.UnreadCount.Should().Be(1);
        chats[1].Title.Should().Be("Группа B-12");
        chats[1].LastMessageAtUtc.Should().BeNull();
        chats[1].Role.Should().Be(ChatMemberRole.Member);
        latest.Select(message => message.Id).Should().Equal(fourth.Id, third.Id);
        latest[1].IsDeleted.Should().BeTrue();
        latest[1].Text.Should().BeNull();
        latest[1].AttachmentIds.Should().BeEmpty();
        earlier.Select(message => message.Text).Should().Equal("Здравствуйте", "Привет");
        earlier[1].SenderName.Should().Be("Смирнова Анна Сергеевна");
        members.Select(member => member.UserId).Should().Equal(vera.Id, boris.Id, anna.Id);
        members[0].Role.Should().Be(ChatMemberRole.Administrator);
        (await queries.IsCurrentMemberAsync(group.Id, anna.Id, CancellationToken)).Should().BeTrue();
        (await queries.IsCurrentMemberAsync(group.Id, Guid.CreateVersion7(), CancellationToken)).Should().BeFalse();
    }
}
