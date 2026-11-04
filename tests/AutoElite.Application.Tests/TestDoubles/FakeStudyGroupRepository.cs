using AutoElite.Application.StudyGroups;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeStudyGroupRepository : InMemoryRepository<StudyGroup>, IStudyGroupRepository
{
    public Task<IReadOnlyList<StudyGroup>> GetWithCurrentMemberAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StudyGroup>>(
            [.. Items.Where(group => group.Enrollments.Any(enrollment => enrollment.ContractId == contractId && enrollment.IsCurrent))]);
}
