namespace AutoElite.Domain.Tests.TestDoubles;

internal sealed class SampleEntity(Guid id) : BaseEntity(id)
{
    public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}
