namespace AutoElite.Domain.Tests.TestDoubles;

internal sealed class SampleEntity : BaseEntity
{
    public SampleEntity(Guid id) : base(id)
    {
    }

    public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}
