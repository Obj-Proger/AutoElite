using AutoElite.Mobile.Session;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The first page, which continues the splash screen while the application chooses where to start: the address of the
/// school on the first start, signing in, or the home page of the signed-in user.
/// </summary>
public partial class StartPage : ContentPage
{
    private readonly UserSession _session;

    /// <summary>Initializes a new instance of the <see cref="StartPage"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    public StartPage(UserSession session)
    {
        InitializeComponent();
        _session = session;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ApiAddress.Saved is null)
            await Routes.GoToAsync(Routes.School);
        else
            await Routes.GoToAsync(await _session.IsSignedInAsync(CancellationToken.None) ? Routes.Home : Routes.SignIn);
    }
}
