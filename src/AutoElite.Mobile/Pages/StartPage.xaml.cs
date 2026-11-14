using AutoElite.Mobile.Session;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The first page, which continues the splash screen while the application chooses where to start: the address of the
/// school on the first start, signing in, or the home page of the signed-in user.
/// </summary>
public partial class StartPage : ContentPage
{
    private readonly UserSession _session;
    private bool _hasChosen;

    /// <summary>Initializes a new instance of the <see cref="StartPage"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    public StartPage(UserSession session)
    {
        InitializeComponent();
        _session = session;
    }

    /// <inheritdoc />
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_hasChosen)
            return;

        // The shell refuses a navigation while its first one, to this page, is still finishing ("Pending Navigations
        // still processing", seen on Windows), so the next page is chosen after it, from the queue of the dispatcher.
        _hasChosen = true;
        Dispatcher.Dispatch(async () => await ChooseAsync());
    }

    private async Task ChooseAsync()
    {
        if (ApiAddress.Saved is null)
            await Routes.GoToAsync(Routes.School);
        else
            await Routes.GoToAsync(await _session.IsSignedInAsync(CancellationToken.None) ? Routes.Home : Routes.SignIn);
    }
}
