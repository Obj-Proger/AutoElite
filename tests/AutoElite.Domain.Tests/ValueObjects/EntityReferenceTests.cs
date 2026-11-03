namespace AutoElite.Domain.Tests.ValueObjects;

public class EntityReferenceTests
{
    private static readonly Guid _id = new("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void Create_ValidValues_ReturnsReference()
    {
        var result = EntityReference.Create(EntityKind.Lead, _id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(EntityKind.Lead);
        result.Value.Id.Should().Be(_id);
        result.Value.ToString().Should().Be("Lead/55555555-5555-5555-5555-555555555555");
    }

    [Fact]
    public void Create_UndefinedKind_ReturnsKindNotDefinedError()
    {
        var result = EntityReference.Create((EntityKind)0, _id);

        result.Error.Should().Be(DomainErrors.EntityReference.KindNotDefined);
    }

    [Fact]
    public void Create_EmptyId_ReturnsIdEmptyError()
    {
        var result = EntityReference.Create(EntityKind.Lead, Guid.Empty);

        result.Error.Should().Be(DomainErrors.EntityReference.IdEmpty);
    }

    [Fact]
    public void Equals_SameKindAndId_AreEqual()
    {
        EntityReference.Create(EntityKind.Lead, _id).Value.Should().Be(EntityReference.Create(EntityKind.Lead, _id).Value);
    }

    [Fact]
    public void Equals_SameIdOtherKind_AreNotEqual()
    {
        EntityReference.Create(EntityKind.Lead, _id).Value.Should().NotBe(EntityReference.Create(EntityKind.Contract, _id).Value);
    }
}
