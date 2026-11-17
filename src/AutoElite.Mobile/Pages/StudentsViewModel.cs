namespace AutoElite.Mobile.Pages;

/// <summary>
/// The students of the school for a manager or an administrator, a tab: found by a part of the name, the email address
/// or the phone number, each opening the card of the student with their contracts and payments.
/// </summary>
public sealed class StudentsViewModel : ViewModel
{
    /// <summary>Initializes a new instance of the <see cref="StudentsViewModel"/> class.</summary>
    /// <param name="search">The search of the students.</param>
    public StudentsViewModel(StudentSearch search)
    {
        ArgumentNullException.ThrowIfNull(search);
        Search = search;
        Search.Chosen = OpenAsync;
    }

    /// <summary>Gets the search of the students.</summary>
    public StudentSearch Search { get; }

    /// <summary>Shows the students, the first ones before anything is typed.</summary>
    /// <returns>A task that completes when they are shown.</returns>
    public Task LoadAsync() => Search.SearchNowAsync();

    private static Task OpenAsync(Guid studentId, string name) =>
        Routes.OpenAsync(
            Routes.ManagedStudent,
            new Dictionary<string, object> { [ManagedStudentViewModel.StudentParameter] = new StudentRequest(studentId, name) });
}
