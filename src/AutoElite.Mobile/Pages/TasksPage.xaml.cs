namespace AutoElite.Mobile.Pages;

/// <summary>The tasks the school gives a member of its staff, opened from the home page.</summary>
public partial class TasksPage : ContentPage
{
    private readonly TasksViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="TasksPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public TasksPage(TasksViewModel viewModel)
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
