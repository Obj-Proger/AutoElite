namespace AutoElite.Mobile.Pages;

/// <summary>A theory lesson to schedule or move, opened over the schedule of a theory instructor.</summary>
public partial class PlanTheoryPage : ContentPage
{
    private readonly PlanTheoryViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="PlanTheoryPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public PlanTheoryPage(PlanTheoryViewModel viewModel)
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
