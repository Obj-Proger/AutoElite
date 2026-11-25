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
        Routing.RegisterRoute(Routes.OfferSlot, typeof(OfferSlotPage));
        Routing.RegisterRoute(Routes.PlanTheory, typeof(PlanTheoryPage));
        Routing.RegisterRoute(Routes.Questions, typeof(QuestionsPage));
        Routing.RegisterRoute(Routes.Question, typeof(QuestionPage));
        Routing.RegisterRoute(Routes.Student, typeof(StudentPage));
        Routing.RegisterRoute(Routes.Exam, typeof(ExamPage));
        Routing.RegisterRoute(Routes.ExamResult, typeof(ExamResultPage));
        Routing.RegisterRoute(Routes.Tasks, typeof(TasksPage));
        Routing.RegisterRoute(Routes.StaffTask, typeof(TaskPage));
        Routing.RegisterRoute(Routes.Documents, typeof(DocumentsPage));
        Routing.RegisterRoute(Routes.Attendance, typeof(AttendancePage));
        Routing.RegisterRoute(Routes.Completion, typeof(CompletionPage));
        Routing.RegisterRoute(Routes.Tests, typeof(TestsPage));
        Routing.RegisterRoute(Routes.TestSession, typeof(TestSessionPage));
        Routing.RegisterRoute(Routes.Notifications, typeof(NotificationsPage));
        Routing.RegisterRoute(Routes.Chat, typeof(ChatPage));
        Routing.RegisterRoute(Routes.NewChat, typeof(NewChatPage));
        Routing.RegisterRoute(Routes.Contracts, typeof(ContractsPage));
        Routing.RegisterRoute(Routes.Security, typeof(SecurityPage));
        Routing.RegisterRoute(Routes.BookStudent, typeof(BookStudentPage));
        Routing.RegisterRoute(Routes.ManagedStudent, typeof(ManagedStudentPage));
        Routing.RegisterRoute(Routes.Payment, typeof(PaymentPage));
        Routing.RegisterRoute(Routes.Profile, typeof(ProfilePage));
        Routing.RegisterRoute(Routes.Personal, typeof(PersonalPage));
        Routing.RegisterRoute(Routes.Lesson, typeof(LessonPage));
        Routing.RegisterRoute(Routes.Lead, typeof(LeadPage));
        Routing.RegisterRoute(Routes.NewLead, typeof(NewLeadPage));
        Routing.RegisterRoute(Routes.Money, typeof(MoneyPage));
        Routing.RegisterRoute(Routes.MoveLesson, typeof(MoveLessonPage));
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
            StudyTab.IsVisible = roles.Contains(UserRole.Student) || roles.Contains(UserRole.TheoryInstructor);
            var isOffice = roles.Contains(UserRole.Manager) || roles.Contains(UserRole.Administrator);
            StudentsTab.IsVisible = isOffice;
            LeadsTab.IsVisible = isOffice;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the profile the tabs stay as they are; the next visit shows them.
        }
    }
}
