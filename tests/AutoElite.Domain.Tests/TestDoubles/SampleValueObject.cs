namespace AutoElite.Domain.Tests.TestDoubles;

internal sealed class SampleValueObject(string name, int? number = null) : ValueObject
{
    public string Name { get; } = name;

    public int? Number { get; } = number;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Number;
    }
}
