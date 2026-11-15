namespace AutoElite.Mobile.Pages;

/// <summary>A new practice slot, opened over the schedule of a driving instructor.</summary>
public partial class OfferSlotPage : ContentPage
{
    private readonly OfferSlotViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="OfferSlotPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public OfferSlotPage(OfferSlotViewModel viewModel)
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
