using System.Reflection;

namespace AutoElite.Domain.Tests.Common;

public class DomainErrorsTests
{
    private static readonly IReadOnlyList<(Type Group, FieldInfo Field, Error Error)> _catalog =
        [.. typeof(DomainErrors).GetNestedTypes()
            .SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(Error))
                .Select(field => (group, field, (Error)field.GetValue(null)!)))];

    [Fact]
    public void Catalog_ContainsErrors()
    {
        _catalog.Should().HaveCountGreaterThan(300);
    }

    [Fact]
    public void Codes_AreGroupDotFieldName()
    {
        var violating = _catalog
            .Where(entry => entry.Error.Code != $"{entry.Group.Name}.{entry.Field.Name}")
            .Select(entry => $"{entry.Group.Name}.{entry.Field.Name} has code {entry.Error.Code}");

        violating.Should().BeEmpty("clients localize messages by code, so a copied code would show the wrong text");
    }

    [Fact]
    public void Codes_AreUnique()
    {
        var duplicates = _catalog
            .GroupBy(entry => entry.Error.Code)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        duplicates.Should().BeEmpty();
    }

    [Fact]
    public void Messages_AreSentencesEndingWithAPeriod()
    {
        var violating = _catalog
            .Where(entry => string.IsNullOrWhiteSpace(entry.Error.Message) || !entry.Error.Message.EndsWith('.'))
            .Select(entry => entry.Error.Code);

        violating.Should().BeEmpty();
    }

    [Fact]
    public void Errors_AreReadOnlyAndCarryNoFieldErrors()
    {
        var violating = _catalog
            .Where(entry => !entry.Field.IsInitOnly || entry.Error.FieldErrors is not null)
            .Select(entry => entry.Error.Code);

        violating.Should().BeEmpty();
    }
}
