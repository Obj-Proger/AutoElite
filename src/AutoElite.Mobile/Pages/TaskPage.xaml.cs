namespace AutoElite.Mobile.Pages;

/// <summary>A task of the staff with what to do with it, opened over the tasks.</summary>
public partial class TaskPage : ContentPage
{
    private readonly TaskViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="TaskPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public TaskPage(TaskViewModel viewModel)
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
