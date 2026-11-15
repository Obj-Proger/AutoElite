using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The studies, a tab of the shell: for a student their practice tests, contracts and documents, for a theory
/// instructor the questions of the school for the tests.
/// </summary>
public partial class StudyPage : ContentPage
{
    private readonly UserSession _session;

    /// <summary>Initializes a new instance of the <see cref="StudyPage"/> class.</summary>
    /// <param name="session">The user of the application, whose roles choose what the page offers.</param>
    public StudyPage(UserSession session)
    {
        _session = session;
        InitializeComponent();
        BindingContext = this;
    }

    /// <summary>Gets a value indicating whether the user is a student.</summary>
    public bool IsStudent
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets a value indicating whether the user is a theory instructor.</summary>
    public bool IsTheoryInstructor
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the command that opens the practice tests.</summary>
    public Command OpenTestsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Tests));

    /// <summary>Gets the command that opens the contracts.</summary>
    public Command OpenContractsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Contracts));

    /// <summary>Gets the command that opens the documents of the student.</summary>
    public Command OpenDocumentsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Documents));

    /// <summary>Gets the command that opens the questions of the school.</summary>
    public Command OpenQuestionsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Questions));

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var roles = (await _session.GetProfileAsync(CancellationToken.None)).Value.Roles ?? [];
            IsStudent = roles.Contains(UserRole.Student);
            IsTheoryInstructor = roles.Contains(UserRole.TheoryInstructor);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the profile the page offers what it offered before.
        }
    }
}
