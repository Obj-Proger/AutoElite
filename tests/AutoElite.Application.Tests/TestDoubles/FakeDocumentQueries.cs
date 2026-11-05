using AutoElite.Application.Documents;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns no documents and records the owner asked for; the reading itself is the infrastructure's job.</summary>
internal sealed class FakeDocumentQueries : IDocumentQueries
{
    public EntityReference? LastOwner { get; private set; }

    public Task<IReadOnlyList<DocumentItem>> GetByOwnerAsync(EntityReference owner, CancellationToken cancellationToken = default)
    {
        LastOwner = owner;

        return Task.FromResult<IReadOnlyList<DocumentItem>>([]);
    }
}
