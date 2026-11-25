namespace AutoElite.Mobile.Pages;

/// <summary>The move of a booked driving lesson to another slot, opened from the card of the student.</summary>
public partial class MoveLessonPage : ContentPage
{
    private readonly MoveLessonViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="MoveLessonPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public MoveLessonPage(MoveLessonViewModel viewModel)
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
