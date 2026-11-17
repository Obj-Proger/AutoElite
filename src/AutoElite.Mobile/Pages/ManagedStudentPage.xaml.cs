namespace AutoElite.Mobile.Pages;

/// <summary>The card of a student for a manager, opened over the students or the schedule.</summary>
public partial class ManagedStudentPage : ContentPage
{
    private readonly ManagedStudentViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ManagedStudentPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ManagedStudentPage(ManagedStudentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
