namespace AutoElite.Architecture.Tests;

/// <summary>
/// The pages of the website need signing in, except the public ones, so that a new page is not open to everyone by
/// mistake; what a page shows is still decided by the API.
/// </summary>
public class WebPagesTests
{
    // The pages that anybody may open: the front page, signing in and the pages of the links that the API emails.
    private static readonly string[] _publicPages =
    [
        "Home.razor",
        "SignIn.razor",
        "ForgotPassword.razor",
        "AcceptInvitation.razor",
        "ResetPassword.razor",
        "ConfirmEmailChange.razor"
    ];

    [Fact]
    public void EveryPageButThePublicOnes_NeedsSigningIn()
    {
        var pages = Directory.EnumerateFiles(Repository.PathOf("src", "AutoElite.Web"), "*.razor", SearchOption.AllDirectories)
            .Select(path => (Name: Path.GetFileName(path), Source: File.ReadAllText(path)))
            .Where(page => page.Source.Contains("@page \"", StringComparison.Ordinal))
            .ToList();

        pages.Select(page => page.Name).Should().Contain(_publicPages);
        pages.Where(page => !_publicPages.Contains(page.Name))
            .Where(page => !page.Source.Contains("@attribute [Authorize", StringComparison.Ordinal))
            .Select(page => page.Name)
            .Should().BeEmpty("a page that is not public needs @attribute [Authorize]");
    }
}
