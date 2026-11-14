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

    /// <summary>The practice tests of a student, opened over the home page.</summary>
    public const string Tests = "tests";

    /// <summary>A practice test attempt, opened over the tests page with the attempt as a parameter.</summary>
    public const string TestSession = "test-session";

    /// <summary>The notifications of the user, opened over the home page.</summary>
    public const string Notifications = "notifications";

    /// <summary>The chats of the user, opened over the home page.</summary>
    public const string Chats = "chats";

    /// <summary>A chat, opened over the chats page with a <see cref="ChatRequest"/>.</summary>
    public const string Chat = "chat";

    /// <summary>The contracts of a student with the progress and the payments, opened over the home page.</summary>
    public const string Contracts = "contracts";

    /// <summary>The settings, opened over the home page.</summary>
    public const string Settings = "settings";

    // How often and how long a navigation waits for one that the shell is still finishing.
    private const int NavigationAttempts = 20;
    private static readonly TimeSpan _navigationRetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Shows a page in place of the current one, with no way back. When the shell is still finishing another
    /// navigation, such as its first one, it refuses a new one ("Pending Navigations still processing"); the
    /// navigation then waits a moment and tries again.
    /// </summary>
    /// <param name="route">The route of the page.</param>
    /// <returns>A task that completes when the page is shown.</returns>
    public static async Task GoToAsync(string route)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await Shell.Current.GoToAsync("//" + route);

                return;
            }
            catch (Exception exception) when (attempt < NavigationAttempts
                && exception.Message.Contains("Pending Navigations", StringComparison.Ordinal))
            {
                await Task.Delay(_navigationRetryDelay);
            }
        }
    }

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
