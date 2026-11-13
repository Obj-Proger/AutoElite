using AutoElite.Client.Authentication;
using AutoElite.Mobile.Pages;

namespace AutoElite.Mobile;

/// <summary>
/// The navigation of the application between its pages. When the API refuses to renew the session, the user is asked
/// to sign in again.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>Initializes a new instance of the <see cref="AppShell"/> class.</summary>
    /// <param name="sessions">The session of the signed-in user.</param>
    public AppShell(AuthSessionManager sessions)
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Booking, typeof(BookingPage));
        Routing.RegisterRoute(Routes.Attendance, typeof(AttendancePage));
        Routing.RegisterRoute(Routes.Completion, typeof(CompletionPage));
        Routing.RegisterRoute(Routes.Tests, typeof(TestsPage));
        Routing.RegisterRoute(Routes.TestSession, typeof(TestSessionPage));
        sessions.SessionEnded += (_, _) => Dispatcher.Dispatch(async () => await Routes.GoToAsync(Routes.SignIn));
    }
}
