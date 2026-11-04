using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.TestDoubles;

internal class InMemoryRepository<TAggregate> : IRepository<TAggregate>
    where TAggregate : BaseEntity
{
    private readonly List<TAggregate> _items = [];

    public IReadOnlyList<TAggregate> Items => _items;

    /// <summary>Adds an aggregate that already exists before the test acts.</summary>
    public void Seed(TAggregate aggregate) => _items.Add(aggregate);

    public Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.SingleOrDefault(item => item.Id == id));

    public void Add(TAggregate aggregate) => _items.Add(aggregate);
}
