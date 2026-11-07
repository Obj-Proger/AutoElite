using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class EntityReferencesTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Exists_RecordOfTheKind_ReturnsTrue_AndForAnotherKindFalse()
    {
        var user = TrainingRecords.Person("Орлов");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), user, course);
        await using (var context = database.CreateContext())
        {
            context.AddRange(user, course, contract);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var references = new EntityReferences(reader);

        (await references.ExistsAsync(EntityReference.Create(EntityKind.User, user.Id).Value, CancellationToken)).Should().BeTrue();
        (await references.ExistsAsync(EntityReference.Create(EntityKind.Contract, contract.Id).Value, CancellationToken))
            .Should().BeTrue();
        (await references.ExistsAsync(EntityReference.Create(EntityKind.CourseTopic, topicId).Value, CancellationToken))
            .Should().BeTrue();
        (await references.ExistsAsync(EntityReference.Create(EntityKind.Lead, user.Id).Value, CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Exists_EveryKind_IsChecked()
    {
        await using var context = database.CreateContext();
        var references = new EntityReferences(context);

        foreach (var kind in Enum.GetValues<EntityKind>())
        {
            var exists = await references.ExistsAsync(EntityReference.Create(kind, Guid.CreateVersion7()).Value, CancellationToken);
            exists.Should().BeFalse(because: $"no {kind} has a new identifier");
        }
    }
}
