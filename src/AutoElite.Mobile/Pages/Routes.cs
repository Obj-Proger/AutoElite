namespace AutoElite.Mobile.Pages;

/// <summary>The routes of the pages in the shell, which the application moves between with absolute routes.</summary>
internal static class Routes
{
    /// <summary>The start page, which chooses the first page.</summary>
    public const string Start = "start";

    /// <summary>The address of the school.</summary>
    public const string School = "school";

    /// <summary>Signing in.</summary>
    public const string SignIn = "sign-in";

    /// <summary>The home page of the signed-in user.</summary>
    public const string Home = "home";

    /// <summary>Shows a page in place of the current one, with no way back.</summary>
    /// <param name="route">The route of the page.</param>
    /// <returns>A task that completes when the page is shown.</returns>
    public static Task GoToAsync(string route) => Shell.Current.GoToAsync("//" + route);
}
