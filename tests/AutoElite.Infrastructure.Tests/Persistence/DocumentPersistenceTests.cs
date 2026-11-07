using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Infrastructure.Outbox;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class DocumentPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static readonly AnswerOptionDraft[] _options = [new("Да", true), new("Нет", false)];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static Document Attach(
        EntityKind kind, Guid ownerId, User uploader, string fileName, DateTime uploadedAtUtc, string? storageKey = null) =>
        Document.Attach(
                EntityReference.Create(kind, ownerId).Value, fileName, "image/png", 2048,
                storageKey ?? $"documents/{Guid.NewGuid():N}", "Скан", uploader.Id, uploadedAtUtc)
            .Value;

    [Fact]
    public async Task Queries_ReadTheDocumentsOfARecord_TheNewestFirst()
    {
        var uploader = TrainingRecords.Person("Белова", "Анна", null);
        var owner = TrainingRecords.Person("Кузнецов");
        var older = Attach(EntityKind.User, owner.Id, uploader, "passport.png", _now);
        var newer = Attach(EntityKind.User, owner.Id, uploader, "medical.png", _now.AddHours(1));
        var otherOwner = Attach(EntityKind.User, uploader.Id, uploader, "photo.png", _now);
        await SaveAsync(uploader, owner, older, newer, otherOwner);
        await using var context = database.CreateContext();

        var documents = await new DocumentQueries(context).GetByOwnerAsync(
            EntityReference.Create(EntityKind.User, owner.Id).Value, CancellationToken);
        var byIds = await new DocumentRepository(context).GetByIdsAsync(
            [older.Id, newer.Id, Guid.CreateVersion7()], CancellationToken);

        documents.Select(document => document.Id).Should().Equal(newer.Id, older.Id);
        var first = documents[0];
        first.FileName.Should().Be("medical.png");
        first.ContentType.Should().Be("image/png");
        first.SizeBytes.Should().Be(2048);
        first.Description.Should().Be("Скан");
        first.UploadedByName.Should().Be("Белова Анна");
        first.UploadedAtUtc.Should().Be(_now.AddHours(1));
        byIds.Select(document => document.Id).Should().BeEquivalentTo(new[] { older.Id, newer.Id });
        byIds.Single(document => document.Id == older.Id).Owner.Should().Be(EntityReference.Create(EntityKind.User, owner.Id).Value);
    }

    [Fact]
    public async Task Remove_DocumentShownAsPhotoAndImage_EmptiesTheReferencesAndQueuesTheFileRemoval()
    {
        var user = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var question = Question.Create(
                "Что означает знак?", null, topicId, [LicenseCategory.B], QuestionSource.School, null, _options)
            .Value;
        await SaveAsync(user, course, question);
        var photo = Attach(EntityKind.User, user.Id, user, "photo.png", _now);
        var image = Attach(EntityKind.Question, question.Id, user, "sign.png", _now);
        await using (var context = database.CreateContext())
        {
            context.AddRange(photo, image);
            (await context.Set<User>().SingleAsync(found => found.Id == user.Id, CancellationToken))
                .SetPhoto(photo.Id).IsSuccess.Should().BeTrue();
            (await context.Set<Question>().SingleAsync(found => found.Id == question.Id, CancellationToken))
                .SetImage(image.Id).IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using (var context = database.CreateContext())
        {
            var repository = new DocumentRepository(context);
            foreach (var document in await repository.GetByIdsAsync([photo.Id, image.Id], CancellationToken))
            {
                document.Delete(_now.AddDays(1)).IsSuccess.Should().BeTrue();
                repository.Remove(document);
            }

            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        (await reader.Set<User>().SingleAsync(found => found.Id == user.Id, CancellationToken)).PhotoDocumentId.Should().BeNull();
        (await reader.Set<Question>().SingleAsync(found => found.Id == question.Id, CancellationToken))
            .ImageDocumentId.Should().BeNull();
        (await reader.Set<Document>().AnyAsync(document => document.Id == photo.Id || document.Id == image.Id, CancellationToken))
            .Should().BeFalse();
        var removals = await reader.Set<OutboxMessage>()
            .Where(message => message.Type.Contains(nameof(DocumentDeletedDomainEvent)))
            .Select(message => message.Payload)
            .ToListAsync(CancellationToken);
        removals.Should().Contain(payload => payload.Contains(photo.StorageKey))
            .And.Contain(payload => payload.Contains(image.StorageKey));
    }

    [Fact]
    public async Task Save_TwoDocumentsWithOneStorageKey_ThrowsConcurrencyConflict()
    {
        var uploader = TrainingRecords.Person("Белова");
        var key = $"documents/{Guid.NewGuid():N}";
        await SaveAsync(uploader, Attach(EntityKind.User, uploader.Id, uploader, "first.png", _now, key));

        var save = () => SaveAsync(Attach(EntityKind.User, uploader.Id, uploader, "second.png", _now, key));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
