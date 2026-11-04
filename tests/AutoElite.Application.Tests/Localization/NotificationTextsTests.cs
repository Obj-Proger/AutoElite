using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoElite.Application.Common.Localization;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Localization;

public partial class NotificationTextsTests
{
    private static readonly IReadOnlyList<string> _names =
        [.. typeof(NotificationTexts).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)];

    private static readonly IReadOnlyList<string> _terms =
    [
        .. Enum.GetValues<LeadSource>().Select(source => NotificationTexts.Term(source).Key),
        .. Enum.GetValues<ExamKind>().Select(kind => NotificationTexts.Term(kind).Key)
    ];

    private static readonly IReadOnlyList<string> _keys =
        [.. _names.Select(name => $"{name}.Title"), .. _names.Select(name => $"{name}.Body"), .. _terms];

    private static Dictionary<string, string> ReadResources(CultureInfo culture) =>
        NotificationTexts.Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    [GeneratedRegex(@"\{(\d+)(:[^}]*)?\}")]
    private static partial Regex PlaceholderPattern();

    /// <summary>The numbers of the placeholders; each language may format a value its own way.</summary>
    private static int[] Placeholders(string text) =>
    [
        .. PlaceholderPattern().Matches(text)
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .Distinct()
            .Order()
    ];

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

    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    public void Bodies_AreValidFormatStrings(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var date = new DateTime(2026, 11, 12, 14, 30, 0);

        foreach (var name in _names)
        {
            var body = () => NotificationTexts.Body(name, culture, date, date, date, date);

            body.Should().NotThrow($"the {cultureName} body of {name} must format");
        }
    }

    [Fact]
    public void Body_WritesDatesAndTermsInTheLanguageOfTheRecipient()
    {
        var start = new DateTime(2026, 11, 12, 14, 30, 0);

        var kind = NotificationTexts.Term(ExamKind.Theory);
        const string name = NotificationTexts.ExamScheduled;

        var russian = NotificationTexts.Body(name, CultureInfo.GetCultureInfo("ru"), kind, "B-12", start);
        var english = NotificationTexts.Body(name, CultureInfo.GetCultureInfo("en"), kind, "B-12", start);

        russian.Should().Be("Теоретический экзамен группы «B-12» назначен на 12 ноября в 14:30.");
        english.Should().Be("Theory exam for group B-12 is scheduled for November 12 at 2:30 PM.");
    }

    [Fact]
    public void Body_FormatsTheAmountInTheCultureOfTheRecipient()
    {
        var body = NotificationTexts.Body(NotificationTexts.PaymentConfirmed, CultureInfo.GetCultureInfo("ru"), 15000m, "RUB", "AE-1");

        body.Should().Contain($"{15000m.ToString("N2", CultureInfo.GetCultureInfo("ru"))} RUB").And.Contain("AE-1");
    }
}
