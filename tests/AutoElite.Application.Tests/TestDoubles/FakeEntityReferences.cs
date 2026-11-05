using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeEntityReferences : IEntityReferences
{
    private readonly HashSet<EntityReference> _existing = [];

    public Task<bool> ExistsAsync(EntityReference reference, CancellationToken cancellationToken = default) =>
        Task.FromResult(_existing.Contains(reference));

    /// <summary>Records a record that exists before the test acts.</summary>
    public void Seed(EntityReference reference) => _existing.Add(reference);
}
