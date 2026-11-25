using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Schedule;
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
    private readonly StudentContracts _contracts;

    /// <summary>Initializes a new instance of the <see cref="StudyPage"/> class.</summary>
    /// <param name="session">The user of the application, whose roles choose what the page offers.</param>
    /// <param name="contracts">The contracts of a student, for their progress.</param>
    public StudyPage(UserSession session, StudentContracts contracts)
    {
        _session = session;
        _contracts = contracts;
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

    /// <summary>Gets the progress of the active contract of a student, or <see langword="null"/>.</summary>
    public ContractFigures? Figures
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFigures));
        }
    }

    /// <summary>Gets a value indicating whether the progress of a contract is shown.</summary>
    public bool HasFigures => Figures is not null;

    /// <summary>Gets the command that opens the practice tests.</summary>
    public Command OpenTestsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Tests));

    /// <summary>Gets the command that opens the practice of the questions.</summary>
    public Command OpenPracticeCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Practice));

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
            Figures = IsStudent ? await ReadFiguresAsync() : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the profile the page offers what it offered before.
        }
    }

    // The progress of the active contract, from the API or the device.
    private async Task<ContractFigures?> ReadFiguresAsync()
    {
        try
        {
            var contracts = (await _contracts.ReadContractsAsync(CancellationToken.None)).Value;
            if (contracts.FirstOrDefault(contract => contract.Status == ContractStatus.Active) is not { Id: { } contractId })
                return null;

            return ContractFigures.Of((await _contracts.ReadDetailsAsync(contractId, CancellationToken.None)).Value);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            return null;
        }
    }
}
