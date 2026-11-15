namespace AutoElite.Mobile.Pages;

/// <summary>The documents of a student, opened from the tab of studies.</summary>
public partial class DocumentsPage : ContentPage
{
    private readonly DocumentsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="DocumentsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public DocumentsPage(DocumentsViewModel viewModel)
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
