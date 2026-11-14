namespace AutoElite.Mobile.Pages;

/// <summary>The studies of a student, a tab of the shell, which opens their practice tests and contracts.</summary>
public partial class StudyPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="StudyPage"/> class.</summary>
    public StudyPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    /// <summary>Gets the command that opens the practice tests.</summary>
    public Command OpenTestsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Tests));

    /// <summary>Gets the command that opens the contracts.</summary>
    public Command OpenContractsCommand { get; } = new(async () => await Routes.OpenAsync(Routes.Contracts));
}
