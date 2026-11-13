using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The address of the school: the user types it, the application checks that the API answers there, keeps it and asks
/// the user to sign in.
/// </summary>
public sealed class SchoolViewModel : ViewModel
{
    private static readonly TimeSpan _checkTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initializes a new instance of the <see cref="SchoolViewModel"/> class.</summary>
    /// <param name="httpClientFactory">Creates the HTTP client that checks the address.</param>
    public SchoolViewModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        Address = ApiAddress.Saved?.ToString() ?? ApiAddress.Suggestion;
        ContinueCommand = new Command(async () => await ContinueAsync(), () => !IsBusy);
    }

    /// <summary>Gets or sets the address the user typed.</summary>
    public string Address
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets what is wrong with the address, or <see langword="null"/>.</summary>
    public string? Error
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>Gets a value indicating whether something is wrong with the address.</summary>
    public bool HasError => Error is not null;

    /// <summary>Gets a value indicating whether the address is being checked.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                ContinueCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that checks and keeps the address.</summary>
    public Command ContinueCommand { get; }

    private async Task ContinueAsync()
    {
        if (!ApiAddress.TryParse(Address, out var address))
        {
            Error = AppTexts.Get("School.InvalidAddress");

            return;
        }

        Error = null;
        IsBusy = true;
        try
        {
            if (!await AnswersAsync(address))
            {
                Error = AppTexts.Get("School.Unreachable");

                return;
            }

            ApiAddress.Save(address);
            await Routes.GoToAsync(Routes.SignIn);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The API answers its liveness probe, which needs no sign-in.
    private async Task<bool> AnswersAsync(Uri address)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = _checkTimeout;
        try
        {
            using var response = await client.GetAsync(new Uri(address, "health/live"));

            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }
}
