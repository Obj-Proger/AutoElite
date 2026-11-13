using System.Text.Json;
using System.Xml.Linq;

namespace AutoElite.Architecture.Tests;

/// <summary>The clients copy the colors of the design system from design/tokens.json; the copies must match.</summary>
public class DesignTokensTests
{
    private static readonly XNamespace _xaml = "http://schemas.microsoft.com/winfx/2009/xaml";

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AutoElite.slnx")))
                directory = directory.Parent;

            return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
        }
    }

    // The colors of the tokens, by their names in PascalCase, as the clients name them.
    private static Dictionary<string, string> TokenColors()
    {
        using var tokens = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "design", "tokens.json")));

        return tokens.RootElement.GetProperty("color").EnumerateObject()
            .Where(token => !token.Name.StartsWith('$'))
            .ToDictionary(
                token => char.ToUpperInvariant(token.Name[0]) + token.Name[1..],
                token => token.Value.GetProperty("$value").GetString()!.ToUpperInvariant());
    }

    [Fact]
    public void MobileColors_AreTheColorsOfTheTokens()
    {
        var path = Path.Combine(RepositoryRoot, "src", "AutoElite.Mobile", "Resources", "Styles", "Colors.xaml");
        var colors = XDocument.Load(path).Root!.Elements()
            .Where(element => element.Name.LocalName == "Color")
            .ToDictionary(element => element.Attribute(_xaml + "Key")!.Value, element => element.Value.Trim().ToUpperInvariant());

        var tokens = TokenColors();

        tokens.Should().NotBeEmpty();
        colors.Should().BeEquivalentTo(tokens);
    }
}
