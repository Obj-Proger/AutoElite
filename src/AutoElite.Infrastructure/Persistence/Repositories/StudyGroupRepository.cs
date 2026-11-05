using AutoElite.Application.StudyGroups;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads study groups with their enrollments, which the capacity and membership rules need.</summary>
internal sealed class StudyGroupRepository(AutoEliteDbContext dbContext)
    : Repository<StudyGroup>(dbContext), IStudyGroupRepository
{
    /// <inheritdoc />
    public override Task<StudyGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Aggregates.Include(group => group.Enrollments).FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<StudyGroup>> GetWithCurrentMemberAsync(
        Guid contractId, CancellationToken cancellationToken = default) =>
        await Aggregates
            .Include(group => group.Enrollments)
            .Where(group => group.Enrollments.Any(enrollment =>
                enrollment.ContractId == contractId && enrollment.WithdrawnAtUtc == null))
            .ToListAsync(cancellationToken);
}
