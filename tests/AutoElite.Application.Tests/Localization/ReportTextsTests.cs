using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoElite.Application.Common.Localization;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Localization;

public partial class ReportTextsTests
{
    private static readonly IReadOnlyList<string> _keys =
    [
        .. typeof(ReportTexts).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!),
        .. Enum.GetValues<LeadSource>().Select(source => $"{nameof(LeadSource)}.{source}"),
        .. Enum.GetValues<LeadStatus>().Select(status => $"{nameof(LeadStatus)}.{status}")
    ];

    private static Dictionary<string, string> ReadResources(CultureInfo culture) =>
        ReportTexts.Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    [GeneratedRegex(@"\{(\d+)(:[^}]*)?\}")]
    private static partial Regex PlaceholderPattern();

    private static int[] Placeholders(string text) =>
    [
        .. PlaceholderPattern().Matches(text)
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .Distinct()
            .Order()
    ];

    [Fact]
    public void English_HasExactlyTheKnownTexts()
    {
        ReadResources(CultureInfo.InvariantCulture).Keys.Should().BeEquivalentTo(_keys);
    }

    [Theory]
    [InlineData("ru")]
    public void Translation_HasEveryTextWithTheSamePlaceholders(string cultureName)
    {
        var english = ReadResources(CultureInfo.InvariantCulture);
        var translation = ReadResources(CultureInfo.GetCultureInfo(cultureName));

        translation.Keys.Should().BeEquivalentTo(_keys);
        foreach (var key in _keys)
        {
            Placeholders(translation[key])
                .Should().Equal(Placeholders(english[key]), $"the {cultureName} text {key} fills the same values");
        }
    }

    [Fact]
    public void Name_WritesEnumValuesInTheCulture()
    {
        ReportTexts.Name(LeadSource.PhoneCall, CultureInfo.GetCultureInfo("ru")).Should().Be("Звонок");
        ReportTexts.Name(LeadStatus.InProgress, CultureInfo.GetCultureInfo("en")).Should().Be("In progress");
    }
}
