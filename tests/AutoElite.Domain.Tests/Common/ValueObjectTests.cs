namespace AutoElite.Domain.Tests.Common;

public class ValueObjectTests
{
    [Fact]
    public void Equality_SameComponents_AreEqual()
    {
        var first = new SampleValueObject("name", 1);
        var second = new SampleValueObject("name", 1);

        first.Equals(second).Should().BeTrue();
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        second.GetHashCode().Should().Be(first.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentComponents_AreNotEqual()
    {
        var first = new SampleValueObject("name", 1);
        var second = new SampleValueObject("name", 2);

        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Equality_SameComponentsButDifferentType_AreNotEqual()
    {
        var first = new SampleValueObject("name", 1);
        var second = new OtherSampleValueObject("name", 1);

        first.Equals(second).Should().BeFalse();
        (first == second).Should().BeFalse();
    }

    [Fact]
    public void Equality_NullComponentOnBothSides_AreEqual()
    {
        var first = new SampleValueObject("name");
        var second = new SampleValueObject("name");

        first.Equals(second).Should().BeTrue();
        second.GetHashCode().Should().Be(first.GetHashCode());
    }

    [Fact]
    public void Equality_NullComponentOnOneSide_AreNotEqual()
    {
        var first = new SampleValueObject("name");
        var second = new SampleValueObject("name", 1);

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void Equality_Null_IsNotEqual()
    {
        var valueObject = new SampleValueObject("name");
        ValueObject? none = null;

        valueObject.Equals(null).Should().BeFalse();
        (valueObject == null).Should().BeFalse();
        (none == valueObject).Should().BeFalse();
        (valueObject != null).Should().BeTrue();
    }

    [Fact]
    public void Equality_BothNull_AreEqual()
    {
        ValueObject? left = null;
        ValueObject? right = null;

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOfAnotherType_IsNotEqual()
    {
        var valueObject = new SampleValueObject("name");

        valueObject.Equals("name").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_NullComponent_DoesNotThrow()
    {
        var valueObject = new SampleValueObject("name");
        var action = () => valueObject.GetHashCode();

        action.Should().NotThrow();
    }

    [Fact]
    public void ValueObject_ImplementsIEquatable()
    {
        var valueObject = new SampleValueObject("name");

        valueObject.Should().BeAssignableTo<IEquatable<ValueObject>>();
    }
}
