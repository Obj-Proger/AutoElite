using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Checks references by the table of their kind. Records are deactivated or marked deleted rather than removed, so a
/// deactivated record still exists. A kind added to <see cref="EntityKind"/> must be added here, or checking it throws.
/// </summary>
internal sealed class EntityReferences(AutoEliteDbContext dbContext) : IEntityReferences
{
    /// <inheritdoc />
    public Task<bool> ExistsAsync(EntityReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var id = reference.Id;

        return reference.Kind switch
        {
            EntityKind.User => Exists<User>(id, cancellationToken),
            EntityKind.Branch => Exists<Branch>(id, cancellationToken),
            EntityKind.Lead => Exists<Lead>(id, cancellationToken),
            EntityKind.Course => Exists<Course>(id, cancellationToken),
            EntityKind.CourseTopic => Exists<CourseTopic>(id, cancellationToken),
            EntityKind.Contract => Exists<Contract>(id, cancellationToken),
            EntityKind.StudyGroup => Exists<StudyGroup>(id, cancellationToken),
            EntityKind.Payment => Exists<Payment>(id, cancellationToken),
            EntityKind.TheoryLesson => Exists<TheoryLesson>(id, cancellationToken),
            EntityKind.DrivingSlot => Exists<DrivingSlot>(id, cancellationToken),
            EntityKind.Vehicle => Exists<Vehicle>(id, cancellationToken),
            EntityKind.TestSession => Exists<TestSession>(id, cancellationToken),
            EntityKind.Exam => Exists<Exam>(id, cancellationToken),
            EntityKind.Chat => Exists<Chat>(id, cancellationToken),
            EntityKind.ChatMessage => Exists<ChatMessage>(id, cancellationToken),
            EntityKind.StaffTask => Exists<StaffTask>(id, cancellationToken),
            EntityKind.Question => Exists<Question>(id, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference.Kind, "The kind of record is not known.")
        };
    }

    private Task<bool> Exists<TEntity>(Guid id, CancellationToken cancellationToken)
        where TEntity : BaseEntity =>
        dbContext.Set<TEntity>().AnyAsync(entity => entity.Id == id, cancellationToken);
}
