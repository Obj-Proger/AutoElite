using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Notifications;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class NotificationPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static EntityReference Reference(EntityKind kind, Guid id) => EntityReference.Create(kind, id).Value;

    private static Notification Notify(
        User recipient, NotificationType type, EntityReference? subject, int minutes, string title = "Новое уведомление") =>
        Notification.Create(recipient.Id, type, title, "Подробности", subject, _now.AddMinutes(minutes)).Value;

    [Fact]
    public async Task Save_Notifications_StoresTheSubjectOrItsAbsence()
    {
        var recipient = TrainingRecords.Person("Орлов");
        var subject = Reference(EntityKind.Lead, Guid.CreateVersion7());
        var about = Notify(recipient, NotificationType.NewLead, subject, 1);
        var general = Notify(recipient, NotificationType.LeadAssigned, null, 2);
        await SaveAsync(recipient, about, general);

        await using var context = database.CreateContext();
        var repository = new NotificationRepository(context);
        var loadedAbout = await repository.GetByIdAsync(about.Id, CancellationToken);
        var loadedGeneral = await repository.GetByIdAsync(general.Id, CancellationToken);

        loadedAbout!.Subject.Should().Be(subject);
        loadedAbout.Body.Should().Be("Подробности");
        loadedGeneral!.Subject.Should().BeNull();
        loadedGeneral.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Save_SecondUnreadNewMessageNotificationOfAChat_ThrowsConcurrencyConflict()
    {
        var recipient = TrainingRecords.Person("Орлов");
        var chat = Reference(EntityKind.Chat, Guid.CreateVersion7());
        var otherChat = Reference(EntityKind.Chat, Guid.CreateVersion7());
        var read = Notify(recipient, NotificationType.NewMessage, chat, 1);
        read.MarkAsRead(_now.AddMinutes(2));
        await SaveAsync(recipient, read, Notify(recipient, NotificationType.NewMessage, chat, 3),
            Notify(recipient, NotificationType.NewMessage, otherChat, 3));

        var save = () => SaveAsync(Notify(recipient, NotificationType.NewMessage, chat, 4));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Queries_ReadThePagesAndTheUnreadCountOfAUser()
    {
        var recipient = TrainingRecords.Person("Орлов");
        var other = TrainingRecords.Person("Андреев");
        var subject = Reference(EntityKind.Lead, Guid.CreateVersion7());
        var first = Notify(recipient, NotificationType.NewLead, subject, 1, "Первое");
        var second = Notify(recipient, NotificationType.LeadAssigned, null, 2, "Второе");
        var third = Notify(recipient, NotificationType.NewLead, null, 3, "Третье");
        first.MarkAsRead(_now.AddMinutes(4));
        await SaveAsync(recipient, other, first, second, third, Notify(other, NotificationType.NewLead, null, 1));
        await using var context = database.CreateContext();
        var queries = new NotificationQueries(context);

        var all = await queries.GetPageAsync(recipient.Id, null, new PageRequest(1, 2), CancellationToken);
        var rest = await queries.GetPageAsync(recipient.Id, null, new PageRequest(2, 2), CancellationToken);
        var read = await queries.GetPageAsync(recipient.Id, true, new PageRequest(1, 20), CancellationToken);
        var unread = await queries.GetPageAsync(recipient.Id, false, new PageRequest(1, 20), CancellationToken);

        all.TotalCount.Should().Be(3);
        all.Items.Select(item => item.Title).Should().Equal("Третье", "Второе");
        all.Items[0].SubjectKind.Should().BeNull();
        all.Items[0].SubjectId.Should().BeNull();
        var oldest = rest.Items.Should().ContainSingle().Which;
        oldest.SubjectKind.Should().Be(EntityKind.Lead);
        oldest.SubjectId.Should().Be(subject.Id);
        oldest.ReadAtUtc.Should().Be(_now.AddMinutes(4));
        read.Items.Select(item => item.Id).Should().Equal(first.Id);
        unread.Items.Select(item => item.Id).Should().Equal(third.Id, second.Id);
        (await queries.CountUnreadAsync(recipient.Id, CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task MarkRead_InBulk_MarksOnlyTheMatchingUnreadNotifications()
    {
        var recipient = TrainingRecords.Person("Орлов");
        var other = TrainingRecords.Person("Андреев");
        var lead = Reference(EntityKind.Lead, Guid.CreateVersion7());
        var early = Notify(recipient, NotificationType.NewLead, lead, 1);
        var late = Notify(recipient, NotificationType.LeadAssigned, lead, 5);
        var unrelated = Notify(recipient, NotificationType.NewLead, Reference(EntityKind.Lead, Guid.CreateVersion7()), 1);
        var ofOther = Notify(other, NotificationType.NewLead, lead, 1);
        await SaveAsync(recipient, other, early, late, unrelated, ofOther);
        var readAt = _now.AddMinutes(10);

        await using (var context = database.CreateContext())
        {
            var repository = new NotificationRepository(context);
            (await repository.MarkReadAboutAsync(recipient.Id, lead, _now.AddMinutes(2), readAt, CancellationToken)).Should().Be(1);
            (await repository.MarkReadAboutAsync(recipient.Id, lead, _now.AddMinutes(2), readAt, CancellationToken)).Should().Be(0);
        }

        await using (var context = database.CreateContext())
        {
            var queries = new NotificationQueries(context);
            (await queries.CountUnreadAsync(recipient.Id, CancellationToken)).Should().Be(2);
            (await queries.CountUnreadAsync(other.Id, CancellationToken)).Should().Be(1);
            (await new NotificationRepository(context).MarkAllAsReadAsync(recipient.Id, readAt, CancellationToken)).Should().Be(2);
            (await queries.CountUnreadAsync(recipient.Id, CancellationToken)).Should().Be(0);
            (await queries.CountUnreadAsync(other.Id, CancellationToken)).Should().Be(1);
        }

        await using var reader = database.CreateContext();
        var loaded = await new NotificationRepository(reader).GetByIdAsync(early.Id, CancellationToken);
        loaded!.ReadAtUtc.Should().Be(readAt);
    }

    [Fact]
    public async Task Repeats_AreFoundByTypeSubjectAndTime()
    {
        var anna = TrainingRecords.Person("Смирнова");
        var boris = TrainingRecords.Person("Белов");
        var id = Guid.CreateVersion7();
        var lesson = Reference(EntityKind.Lead, id);
        var sameIdOtherKind = Reference(EntityKind.Contract, id);
        var recent = Notify(anna, NotificationType.NewLead, lesson, 10);
        var old = Notify(boris, NotificationType.NewLead, lesson, 1);
        var otherType = Notify(boris, NotificationType.LeadAssigned, lesson, 10);
        var otherKind = Notify(boris, NotificationType.NewLead, sameIdOtherKind, 10);
        recent.MarkAsRead(_now.AddMinutes(11));
        await SaveAsync(anna, boris, recent, old, otherType, otherKind);
        await using var context = database.CreateContext();
        var repository = new NotificationRepository(context);

        var sent = await repository.GetSentSinceAsync(NotificationType.NewLead, [lesson], _now.AddMinutes(5), CancellationToken);
        var none = await repository.GetSentSinceAsync(NotificationType.NewLead, [], _now, CancellationToken);
        var withUnread = await repository.GetRecipientsWithUnreadAsync(
            [anna.Id, boris.Id], NotificationType.NewLead, lesson, CancellationToken);

        sent.Should().Equal(new SentNotification(anna.Id, lesson, _now.AddMinutes(10)));
        none.Should().BeEmpty();
        withUnread.Should().BeEquivalentTo(new[] { boris.Id });
    }
}
