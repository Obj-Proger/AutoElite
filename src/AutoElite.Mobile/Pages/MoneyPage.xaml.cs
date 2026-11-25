namespace AutoElite.Mobile.Pages;

/// <summary>The payments waiting for their money and the debts of the students, opened from the home page of the office.</summary>
public partial class MoneyPage : ContentPage
{
    private readonly MoneyViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="MoneyPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public MoneyPage(MoneyViewModel viewModel)
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
