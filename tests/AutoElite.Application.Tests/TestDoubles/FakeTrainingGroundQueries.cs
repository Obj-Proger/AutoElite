using AutoElite.Application.Common.Authorization;
using AutoElite.Application.TrainingGrounds;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded grounds and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeTrainingGroundQueries : ITrainingGroundQueries
{
    private readonly Dictionary<Guid, TrainingGroundItem> _grounds = [];

    public (Guid? BranchId, bool? IsActive, DataScope Scope)? LastListCall { get; private set; }

    public void Seed(TrainingGroundItem ground) => _grounds.Add(ground.Id, ground);

    public Task<IReadOnlyList<TrainingGroundItem>> GetListAsync(
        Guid? branchId, bool? isActive, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastListCall = (branchId, isActive, scope);

        return Task.FromResult<IReadOnlyList<TrainingGroundItem>>([.. _grounds.Values]);
    }

    public Task<TrainingGroundItem?> GetAsync(Guid groundId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_grounds.GetValueOrDefault(groundId));
}
