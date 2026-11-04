using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Tests.Localization;

public partial class NotificationTextsTests
{
    private static readonly IReadOnlyList<string> _names =
        [.. typeof(NotificationTexts).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)];

    private static readonly IReadOnlyList<string> _keys =
        [.. _names.Select(name => $"{name}.Title"), .. _names.Select(name => $"{name}.Body")];

    private static Dictionary<string, string> ReadResources(CultureInfo culture) =>
        NotificationTexts.Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex PlaceholderPattern();

    private static string[] Placeholders(string text) =>
        [.. PlaceholderPattern().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal)];

    [Fact]
    public void English_HasATitleAndABodyForEveryNotification()
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
    public void Titles_HaveNoPlaceholders()
    {
        var english = ReadResources(CultureInfo.InvariantCulture);

        foreach (var name in _names)
            Placeholders(english[$"{name}.Title"]).Should().BeEmpty($"the title of {name} is shown as it is");
    }

    [Fact]
    public void Body_FormatsTheAmountInTheCultureOfTheRecipient()
    {
        var body = NotificationTexts.Body(NotificationTexts.PaymentConfirmed, CultureInfo.GetCultureInfo("ru"), 15000m, "RUB", "AE-1");

        body.Should().Contain($"{15000m.ToString("N2", CultureInfo.GetCultureInfo("ru"))} RUB").And.Contain("AE-1");
    }
}
