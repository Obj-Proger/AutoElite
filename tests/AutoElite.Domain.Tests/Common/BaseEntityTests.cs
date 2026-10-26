namespace AutoElite.Domain.Tests.Common;

public class BaseEntityTests
{
    private static readonly Guid _id = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _otherId = new("22222222-2222-2222-2222-222222222222");
    private static readonly SampleDomainEvent _firstEvent = new(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    private static readonly SampleDomainEvent _secondEvent = new(new DateTime(2026, 1, 1, 12, 5, 0, DateTimeKind.Utc));

    [Fact]
    public void Constructor_WithId_SetsId()
    {
        var entity = new SampleEntity(_id);

        entity.Id.Should().Be(_id);
    }

    [Fact]
    public void DomainEvents_NewEntity_IsEmpty()
    {
        var entity = new SampleEntity(_id);

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_AddsEvent()
    {
        var entity = new SampleEntity(_id);

        entity.Raise(_firstEvent);

        entity.DomainEvents.Should().ContainSingle().Which.Should().Be(_firstEvent);
    }

    [Fact]
    public void RaiseDomainEvent_MultipleEvents_KeepsRaisedOrder()
    {
        var entity = new SampleEntity(_id);

        entity.Raise(_firstEvent);
        entity.Raise(_secondEvent);

        entity.DomainEvents.Should().Equal(_firstEvent, _secondEvent);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var entity = new SampleEntity(_id);
        entity.Raise(_firstEvent);
        entity.Raise(_secondEvent);

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_CannotBeModifiedThroughReturnedCollection()
    {
        var entity = new SampleEntity(_id);
        var events = (IList<IDomainEvent>)entity.DomainEvents;
        var action = () => events.Add(_firstEvent);

        action.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Equality_SameIdAndType_AreEqual()
    {
        var first = new SampleEntity(_id);
        var second = new SampleEntity(_id);

        first.Equals(second).Should().BeTrue();
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        second.GetHashCode().Should().Be(first.GetHashCode());
    }

    [Fact]
    public void Equality_SameInstance_IsEqual()
    {
        var entity = new SampleEntity(_id);

        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentIds_AreNotEqual()
    {
        var first = new SampleEntity(_id);
        var second = new SampleEntity(_otherId);

        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Equality_SameIdButDifferentType_AreNotEqual()
    {
        var first = new SampleEntity(_id);
        var second = new AnotherSampleEntity(_id);

        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
    }

    [Fact]
    public void Equality_Null_IsNotEqual()
    {
        var entity = new SampleEntity(_id);
        BaseEntity? none = null;

        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (none == entity).Should().BeFalse();
        (entity != null).Should().BeTrue();
    }

    [Fact]
    public void Equality_BothNull_AreEqual()
    {
        BaseEntity? left = null;
        BaseEntity? right = null;

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOfAnotherType_IsNotEqual()
    {
        var entity = new SampleEntity(_id);

        entity.Equals("not an entity").Should().BeFalse();
    }
}
