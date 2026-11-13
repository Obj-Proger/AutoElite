using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AutoElite.Architecture.Tests;

/// <summary>
/// The texts of the website are in WebTexts.resx, in English, and translated in WebTexts.ru.resx; the components take
/// them by key, as <c>L["Key"]</c>. Every key used has a text in every language, and every text is used.
/// </summary>
public partial class WebTextsTests
{
    private static readonly string _web = Repository.PathOf("src", "AutoElite.Web");

    private static Dictionary<string, string> Texts(string culture)
    {
        var path = Path.Combine(_web, "Resources", $"WebTexts{culture}.resx");

        return XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(data => data.Attribute("name")!.Value, data => data.Element("value")!.Value);
    }

    private static HashSet<string> UsedKeys()
    {
        var separator = Path.DirectorySeparatorChar;
        var sources = Directory.EnumerateFiles(_web, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.Ordinal) || path.EndsWith(".cs", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{separator}obj{separator}", StringComparison.Ordinal)
                && !path.Contains($"{separator}bin{separator}", StringComparison.Ordinal));

        return
        [
            .. sources.Select(path => File.ReadAllText(path))
                .SelectMany(source => KeyUse().Matches(source))
                .Select(match => match.Groups["key"].Value)
        ];
    }

    private static string[] Placeholders(string text) =>
        [.. Placeholder().Matches(text).Select(match => match.Value).Distinct().Order(StringComparer.Ordinal)];

    [GeneratedRegex(@"L\[""(?<key>[A-Za-z]+\.[A-Za-z]+)""\]")]
    private static partial Regex KeyUse();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    [Fact]
    public void Translation_HasTheKeysAndPlaceholdersOfTheEnglishTexts()
    {
        var english = Texts(string.Empty);
        var russian = Texts(".ru");

        english.Should().NotBeEmpty();
        russian.Keys.Should().BeEquivalentTo(english.Keys);
        russian.Values.Should().NotContain(text => string.IsNullOrWhiteSpace(text));
        foreach (var (key, text) in english)
            Placeholders(russian[key]).Should().Equal(Placeholders(text), "the text {0} is filled in the same way in every language", key);
    }

    [Fact]
    public void UsedKeys_HaveTexts_AndEveryTextIsUsed()
    {
        var used = UsedKeys();
        var keys = Texts(string.Empty).Keys;

        used.Should().NotBeEmpty();
        used.Should().BeSubsetOf(keys);
        keys.Should().BeSubsetOf(used);
    }
}
