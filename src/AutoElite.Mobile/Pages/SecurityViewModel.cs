using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The password and the email address of the user. A new password needs the current one and is typed twice; the other
/// devices are then signed out and this one stays signed in. A new email address needs the current password too, and
/// the API sends a link to it, which changes the address once followed.
/// </summary>
/// <param name="account">The account of the user.</param>
public sealed class SecurityViewModel(UserAccount account) : ViewModel
{
    /// <summary>Gets or sets the current password, for a new password.</summary>
    public string CurrentPassword
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the new password.</summary>
    public string NewPassword
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the new password typed again.</summary>
    public string RepeatedPassword
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how changing the password went, or <see langword="null"/>.</summary>
    public string? PasswordMessage
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasPasswordMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message about the password.</summary>
    public bool HasPasswordMessage => PasswordMessage is not null;

    /// <summary>Gets or sets the new email address.</summary>
    public string NewEmail
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the current password, for a new email address.</summary>
    public string EmailPassword
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how asking for a new email address went, or <see langword="null"/>.</summary>
    public string? EmailMessage
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasEmailMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message about the email address.</summary>
    public bool HasEmailMessage => EmailMessage is not null;

    /// <summary>Gets a value indicating whether a change is being sent.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            ChangePasswordCommand.ChangeCanExecute();
            ChangeEmailCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that changes the password.</summary>
    public Command ChangePasswordCommand => field ??= new Command(async () => await ChangePasswordAsync(), () => !IsBusy);

    /// <summary>Gets the command that asks for a new email address.</summary>
    public Command ChangeEmailCommand => field ??= new Command(async () => await ChangeEmailAsync(), () => !IsBusy);

    // Sends a change; a refusal of the API or no network becomes the message, and true tells that it was done.
    private async Task<bool> SendAsync(Func<Task> change, Action<string> showProblem)
    {
        IsBusy = true;
        try
        {
            await change();

            return true;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            showProblem(ProblemTexts.Of(exception));

            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword))
        {
            PasswordMessage = AppTexts.Get("Password.Missing");

            return;
        }

        if (NewPassword != RepeatedPassword)
        {
            PasswordMessage = AppTexts.Get("Password.Mismatch");

            return;
        }

        PasswordMessage = null;
        if (await SendAsync(
                () => account.ChangePasswordAsync(CurrentPassword, NewPassword, CancellationToken.None),
                problem => PasswordMessage = problem))
        {
            CurrentPassword = NewPassword = RepeatedPassword = string.Empty;
            PasswordMessage = AppTexts.Get("Security.PasswordChanged");
        }
    }

    private async Task ChangeEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(NewEmail) || string.IsNullOrEmpty(EmailPassword))
        {
            EmailMessage = AppTexts.Get("Security.EmailMissing");

            return;
        }

        EmailMessage = null;
        var email = NewEmail.Trim();
        if (await SendAsync(
                () => account.RequestEmailChangeAsync(email, EmailPassword, CancellationToken.None),
                problem => EmailMessage = problem))
        {
            NewEmail = EmailPassword = string.Empty;
            EmailMessage = AppTexts.Format("Security.EmailSent", email);
        }
    }
}
