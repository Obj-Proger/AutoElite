using AutoElite.Client.Api.Models;
using AutoElite.Client.Authentication;
using AutoElite.Mobile.Pages;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile;

/// <summary>
/// The navigation of the application between its pages: the tabs of the signed-in user, which show what their roles
/// may do, and the pages that open over them. When the API refuses to renew the session, the user is asked to sign in
/// again.
/// </summary>
public partial class AppShell : Shell
{
    private readonly UserSession _session;
    private bool _hasShownTabs;

    /// <summary>Initializes a new instance of the <see cref="AppShell"/> class.</summary>
    /// <param name="sessions">The session of the signed-in user.</param>
    /// <param name="session">The user of the application, whose roles choose the tabs.</param>
    public AppShell(AuthSessionManager sessions, UserSession session)
    {
        InitializeComponent();
        _session = session;
        Routing.RegisterRoute(Routes.Booking, typeof(BookingPage));
        Routing.RegisterRoute(Routes.Attendance, typeof(AttendancePage));
        Routing.RegisterRoute(Routes.Completion, typeof(CompletionPage));
        Routing.RegisterRoute(Routes.Tests, typeof(TestsPage));
        Routing.RegisterRoute(Routes.TestSession, typeof(TestSessionPage));
        Routing.RegisterRoute(Routes.Notifications, typeof(NotificationsPage));
        Routing.RegisterRoute(Routes.Chat, typeof(ChatPage));
        Routing.RegisterRoute(Routes.Contracts, typeof(ContractsPage));
        Routing.RegisterRoute(Routes.Security, typeof(SecurityPage));
        sessions.SessionEnded += (_, _) => Dispatcher.Dispatch(async () => await Routes.GoToAsync(Routes.SignIn));
    }

    /// <inheritdoc />
    protected override async void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Once each time the user comes to the tabs, after signing in or starting the application.
        if (CurrentItem?.Route != Routes.Main)
        {
            _hasShownTabs = false;

            return;
        }

        if (_hasShownTabs)
            return;

        _hasShownTabs = true;
        await ShowTabsAsync();
    }

    // Shows the tabs that the roles of the signed-in user use, from the profile on the API or on the device.
    private async Task ShowTabsAsync()
    {
        try
        {
            var roles = (await _session.GetProfileAsync(CancellationToken.None)).Value.Roles ?? [];
            StudyTab.IsVisible = roles.Contains(UserRole.Student);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the profile the tabs stay as they are; the next visit shows them.
        }
    }
}
