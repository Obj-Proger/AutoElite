using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AutoElite.Architecture.Tests;

/// <summary>
/// The clients copy the colors of the design system from design/tokens.json, and the website its fonts from
/// design/fonts; the copies must match.
/// </summary>
public partial class DesignTokensTests
{
    private static readonly XNamespace _xaml = "http://schemas.microsoft.com/winfx/2009/xaml";

    // The colors of the tokens, by their names in PascalCase, as the clients name them.
    private static Dictionary<string, string> TokenColors()
    {
        using var tokens = JsonDocument.Parse(File.ReadAllText(Repository.PathOf("design", "tokens.json")));

        return tokens.RootElement.GetProperty("color").EnumerateObject()
            .Where(token => !token.Name.StartsWith('$'))
            .ToDictionary(
                token => char.ToUpperInvariant(token.Name[0]) + token.Name[1..],
                token => token.Value.GetProperty("$value").GetString()!.ToUpperInvariant());
    }

    [Fact]
    public void MobileColors_AreTheColorsOfTheTokens()
    {
        var path = Repository.PathOf("src", "AutoElite.Mobile", "Resources", "Styles", "Colors.xaml");
        var colors = XDocument.Load(path).Root!.Elements()
            .Where(element => element.Name.LocalName == "Color")
            .ToDictionary(element => element.Attribute(_xaml + "Key")!.Value, element => element.Value.Trim().ToUpperInvariant());

        var tokens = TokenColors();

        tokens.Should().NotBeEmpty();
        colors.Should().BeEquivalentTo(tokens);
    }

    [Fact]
    public void WebColors_AreTheColorsOfTheTokens()
    {
        var css = File.ReadAllText(Repository.PathOf("src", "AutoElite.Web", "wwwroot", "css", "tokens.css"));
        var colors = CssColor().Matches(css).ToDictionary(
            match => string.Concat(match.Groups["name"].Value.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..])),
            match => match.Groups["value"].Value.ToUpperInvariant());

        colors.Should().BeEquivalentTo(TokenColors());
    }

    [Fact]
    public void WebFonts_AreTheFontsOfTheDesign()
    {
        var design = Repository.PathOf("design", "fonts");
        var web = Repository.PathOf("src", "AutoElite.Web", "wwwroot", "fonts");
        var designFiles = Directory.GetFiles(design).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();

        Directory.GetFiles(web).Select(Path.GetFileName).Order(StringComparer.Ordinal).Should().Equal(designFiles);
        foreach (var file in designFiles)
        {
            File.ReadAllBytes(Path.Combine(web, file!)).AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(design, file!)))
                .Should().BeTrue("the website serves {0} as design/fonts has it", file);
        }
    }

    // A color of the tokens as a custom property, such as --color-surface-raised: #23252A.
    [GeneratedRegex(@"--color-(?<name>[a-z-]+):\s*(?<value>#[0-9A-Fa-f]{6})")]
    private static partial Regex CssColor();
}
