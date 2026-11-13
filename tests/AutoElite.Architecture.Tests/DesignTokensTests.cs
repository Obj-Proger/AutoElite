using System.Text.Json;
using System.Xml.Linq;

namespace AutoElite.Architecture.Tests;

/// <summary>The clients copy the colors of the design system from design/tokens.json; the copies must match.</summary>
public class DesignTokensTests
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
}
