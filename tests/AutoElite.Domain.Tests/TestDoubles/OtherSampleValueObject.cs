namespace AutoElite.Domain.Tests.TestDoubles;

internal sealed class OtherSampleValueObject : ValueObject
{
    public OtherSampleValueObject(string name, int? number = null)
    {
        Name = name;
        Number = number;
    }

    public string Name { get; }

    public int? Number { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Number;
    }
}
