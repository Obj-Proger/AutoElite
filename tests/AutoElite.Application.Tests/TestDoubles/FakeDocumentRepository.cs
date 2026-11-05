using AutoElite.Application.Documents;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeDocumentRepository : InMemoryRepository<Document>, IDocumentRepository
{
    public Task<IReadOnlyList<Document>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Document>>([.. Items.Where(document => ids.Contains(document.Id))]);
}
