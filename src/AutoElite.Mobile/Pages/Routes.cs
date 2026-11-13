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

    /// <summary>Booking a driving lesson, opened over the home page.</summary>
    public const string Booking = "booking";

    /// <summary>The attendance at a theory lesson, opened over the home page with the lesson in the query.</summary>
    public const string Attendance = "attendance";

    /// <summary>Completing a driving lesson, opened over the home page with a <see cref="CompletionRequest"/>.</summary>
    public const string Completion = "completion";

    /// <summary>Shows a page in place of the current one, with no way back.</summary>
    /// <param name="route">The route of the page.</param>
    /// <returns>A task that completes when the page is shown.</returns>
    public static Task GoToAsync(string route) => Shell.Current.GoToAsync("//" + route);

    /// <summary>Opens a page over the current one, which the back button returns to.</summary>
    /// <param name="route">The route of the page.</param>
    /// <returns>A task that completes when the page is shown.</returns>
    public static Task OpenAsync(string route) => Shell.Current.GoToAsync(route);

    /// <summary>Opens a page over the current one and passes it parameters, which need no escaping.</summary>
    /// <param name="route">The route of the page.</param>
    /// <param name="parameters">The parameters, which the page receives in <see cref="IQueryAttributable"/>.</param>
    /// <returns>A task that completes when the page is shown.</returns>
    public static Task OpenAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(route, parameters);
}
