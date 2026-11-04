using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Tests.Localization;

public partial class EmailTextsTests
{
    private static readonly IReadOnlyList<string> _keys =
        [.. typeof(EmailTexts).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)];

    private static Dictionary<string, string> ReadResources(CultureInfo culture) =>
        EmailTexts.Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();

    private static string[] Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal)];

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
            Placeholders(translation[key]).Should().Equal(Placeholders(english[key]), $"the {cultureName} text {key} fills the same values");
    }

    [Fact]
    public void Format_FillsThePlaceholdersInTheCulture()
    {
        var body = EmailTexts.Format(EmailTexts.InvitationBody, CultureInfo.GetCultureInfo("en"), "Anna", "https://app.test/x");

        body.Should().StartWith("Hello, Anna!").And.Contain("https://app.test/x");
    }

    [Fact]
    public void Format_UnknownKey_Throws()
    {
        var act = () => EmailTexts.Format("Unknown.Text", CultureInfo.InvariantCulture);

        act.Should().Throw<InvalidOperationException>();
    }
}
