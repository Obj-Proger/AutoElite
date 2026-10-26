namespace AutoElite.Domain.Tests.TestDoubles;

internal sealed class SampleValueObject : ValueObject
{
    public SampleValueObject(string name, int? number = null)
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
