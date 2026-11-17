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

    /// <summary>The tabs of the signed-in user.</summary>
    public const string Main = "main";

    /// <summary>The home page of the signed-in user, the first tab.</summary>
    public const string Home = Main + "/home";

    /// <summary>The schedule of the signed-in user, a tab.</summary>
    public const string Schedule = Main + "/schedule";

    /// <summary>A new practice slot of a driving instructor, opened over the schedule with a <see cref="SlotRequest"/>.</summary>
    public const string OfferSlot = "offer-slot";

    /// <summary>
    /// A theory lesson to schedule or move, opened over the schedule or the home page with a <see cref="TheoryLessonRequest"/>.
    /// </summary>
    public const string PlanTheory = "plan-theory";

    /// <summary>The questions of the school for the practice tests, opened over the tab of studies.</summary>
    public const string Questions = "questions";

    /// <summary>A question to write, or to correct with its identifier as a parameter, opened over the questions.</summary>
    public const string Question = "question";

    /// <summary>The progress of a student for their instructor, opened with a <see cref="StudentRequest"/>.</summary>
    public const string Student = "student";

    /// <summary>An exam with its members and results, opened over the schedule with its identifier.</summary>
    public const string Exam = "exam";

    /// <summary>A result of an exam to record or correct, opened over the exam with an <see cref="ExamResultRequest"/>.</summary>
    public const string ExamResult = "exam-result";

    /// <summary>The tasks of a member of the staff, opened over the home page.</summary>
    public const string Tasks = "tasks";

    /// <summary>A task of the staff, opened over the tasks with its identifier.</summary>
    public const string StaffTask = "task";

    /// <summary>The documents of a student, opened over the tab of studies.</summary>
    public const string Documents = "documents";

    /// <summary>Booking a driving lesson, opened over the home page.</summary>
    public const string Booking = "booking";

    /// <summary>The attendance at a theory lesson, opened over the home page with the lesson in the query.</summary>
    public const string Attendance = "attendance";

    /// <summary>Completing a driving lesson, opened over the home page with a <see cref="CompletionRequest"/>.</summary>
    public const string Completion = "completion";

    /// <summary>The practice tests of a student, opened over the tab of studies.</summary>
    public const string Tests = "tests";

    /// <summary>A practice test attempt, opened over the tests page with the attempt as a parameter.</summary>
    public const string TestSession = "test-session";

    /// <summary>The notifications of the user, opened over the home page.</summary>
    public const string Notifications = "notifications";

    /// <summary>A chat, opened over the tab of chats with a <see cref="ChatRequest"/>.</summary>
    public const string Chat = "chat";

    /// <summary>The people to start a personal chat with, opened over the tab of chats.</summary>
    public const string NewChat = "new-chat";

    /// <summary>The contracts of a student with the progress and the payments, opened over the tab of studies.</summary>
    public const string Contracts = "contracts";

    /// <summary>The change of the password and of the email address, opened over the tab of the profile.</summary>
    public const string Security = "security";

    /// <summary>Booking a student on a free slot by a manager, opened over the schedule with a <see cref="BookStudentRequest"/>.</summary>
    public const string BookStudent = "book-student";

    /// <summary>
    /// The card of a student for a manager, opened over the students or the schedule with a <see cref="StudentRequest"/>.
    /// </summary>
    public const string ManagedStudent = "managed-student";

    /// <summary>A payment a manager records, opened over the card of the student with a <see cref="PaymentRequest"/>.</summary>
    public const string Payment = "payment";

    /// <summary>The leads of a manager, opened over the home page.</summary>
    public const string Leads = "leads";

    /// <summary>A lead for a manager, opened over the leads with a <see cref="LeadRequest"/>.</summary>
    public const string Lead = "lead";

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

    /// <summary>
    /// Shows a page in place of the one opened over the tabs, with its parameters, so that the back button skips the
    /// page that chose it.
    /// </summary>
    /// <param name="route">The route of the page.</param>
    /// <param name="parameters">The parameters, which the page receives in <see cref="IQueryAttributable"/>.</param>
    /// <returns>A task that completes when the page is shown.</returns>
    public static Task ReplaceAsync(string route, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync("../" + route, parameters);
}
