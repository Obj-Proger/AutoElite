using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AutoElite.Architecture.Tests;

/// <summary>
/// The texts of the mobile application are in AppTexts.resx, in English, and translated in AppTexts.ru.resx; the pages
/// and the code take them by key. Every key used has a text in every language, and every text is used.
/// </summary>
public partial class MobileTextsTests
{
    private static readonly string _mobile = Repository.PathOf("src", "AutoElite.Mobile");

    private static Dictionary<string, string> Texts(string culture)
    {
        var path = Path.Combine(_mobile, "Resources", "Strings", $"AppTexts{culture}.resx");

        return XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(data => data.Attribute("name")!.Value, data => data.Element("value")!.Value);
    }

    // The keys the pages ({l:Text Key}) and the code (AppTexts.Get("Key"), AppTexts.Format("Key", ...)) use.
    private static HashSet<string> UsedKeys()
    {
        var separator = Path.DirectorySeparatorChar;
        var sources = Directory.EnumerateFiles(_mobile, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".xaml", StringComparison.Ordinal) || path.EndsWith(".cs", StringComparison.Ordinal))
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

    [GeneratedRegex("""(?:\{l:Text\s+|AppTexts\.(?:Get|Format)\(\s*")(?<key>[A-Za-z]+\.[A-Za-z]+)""")]
    private static partial Regex KeyUse();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    [Fact]
    public void Translation_HasTheKeysOfTheEnglishTexts()
    {
        var english = Texts(string.Empty);
        var russian = Texts(".ru");

        english.Should().NotBeEmpty();
        russian.Keys.Should().BeEquivalentTo(english.Keys);
        russian.Values.Should().NotContain(text => string.IsNullOrWhiteSpace(text));
        english.Values.Should().NotContain(text => string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void Translation_HasThePlaceholdersOfTheEnglishTexts()
    {
        var english = Texts(string.Empty);
        var russian = Texts(".ru");

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
