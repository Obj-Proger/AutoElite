namespace AutoElite.Mobile.Pages;

/// <summary>Booking a driving lesson, opened from the home page of a student.</summary>
public partial class BookingPage : ContentPage
{
    private readonly BookingViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="BookingPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public BookingPage(BookingViewModel viewModel)
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
