using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The people the user can start a personal chat with: their instructors, or the students they teach, found by a part
/// of the name. Choosing one opens the chat with them, which the API creates on first use.
/// </summary>
/// <param name="chats">The chats.</param>
public sealed class NewChatViewModel(Chats chats) : ViewModel
{
    private IReadOnlyList<(ChatContactItem Contact, ContactEntry Entry)> _all = [];

    /// <summary>Gets the people that match the search.</summary>
    public IReadOnlyList<ContactEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets a part of the name to find.</summary>
    public string Search
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                Filter();
        }
    } = string.Empty;

    /// <summary>Gets that there is nobody yet, or that the list is from the device, or why opening failed.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice to show.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets a value indicating whether a chat is being opened.</summary>
    public bool IsBusy
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Reads the people.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var contacts = await chats.ReadContactsAsync(CancellationToken.None);
            _all = [.. contacts.Value.Where(contact => contact.UserId is not null).Select(contact => (contact, Entry(contact)))];
            Filter();
            Notice = contacts.IsFromCache
                ? AppTexts.Format("Chats.Offline", contacts.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : _all.Count == 0 ? AppTexts.Get("NewChat.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    // Initials of a full name, its first two words: the last and the first name.
    private static string InitialsOf(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]))
            .ToUpper(CultureInfo.CurrentCulture);

    private ContactEntry Entry(ChatContactItem contact)
    {
        var name = contact.FullName ?? string.Empty;
        var userId = contact.UserId!.Value;

        return new ContactEntry
        {
            Name = name,
            Initials = InitialsOf(name),
            Roles = string.Join(", ", (contact.Roles ?? []).OfType<UserRole>().Select(RoleTexts.Of)),
            OpenCommand = new Command(async () => await OpenAsync(userId, name))
        };
    }

    private void Filter()
    {
        var search = Search.Trim();
        Items =
        [
            .. _all
                .Where(item => search.Length == 0
                    || CultureInfo.CurrentCulture.CompareInfo.IndexOf(item.Entry.Name, search, CompareOptions.IgnoreCase) >= 0)
                .Select(item => item.Entry)
        ];
    }

    private async Task OpenAsync(Guid userId, string name)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            var chatId = await chats.OpenDirectAsync(userId, CancellationToken.None);
            await Routes.ReplaceAsync(
                Routes.Chat, new Dictionary<string, object> { [ChatViewModel.ChatParameter] = new ChatRequest(chatId, name) });
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException)
        {
            Notice = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
