using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The search of the students for a manager: what is typed is looked up a moment after the typing stops, and only the
/// latest search shows, however the answers of the API arrive; the students found open what <see cref="Chosen"/> does.
/// </summary>
/// <param name="directory">The students of the school.</param>
public sealed class StudentSearch(StudentDirectory directory) : ViewModel
{
    // How long the typing has to stop before the search goes to the API.
    private static readonly TimeSpan _delay = TimeSpan.FromMilliseconds(350);
    private int _version;

    /// <summary>Gets or sets what is done with a student found: their identifier and full name.</summary>
    public Func<Guid, string, Task>? Chosen { get; set; }

    /// <summary>Gets or sets a part of the name, the email address or the phone number to find.</summary>
    public string Text
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _ = SearchAsync(_delay);
        }
    } = string.Empty;

    /// <summary>Gets the students found.</summary>
    public IReadOnlyList<ContactEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets that nobody was found, or why the search failed.</summary>
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

    /// <summary>Looks up what is typed at once, such as the first students when the page opens.</summary>
    /// <returns>A task that completes when the students are shown.</returns>
    public Task SearchNowAsync() => SearchAsync(TimeSpan.Zero);

    private async Task SearchAsync(TimeSpan delay)
    {
        var version = ++_version;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay);

        if (version != _version)
            return;

        try
        {
            var students = await directory.SearchAsync(Text, CancellationToken.None);
            if (version != _version)
                return;

            Items = [.. students.Where(student => student.Id is not null).Select(Entry)];
            Notice = Items.Count == 0 ? AppTexts.Get("Students.NotFound") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            if (version != _version)
                return;

            Items = [];
            Notice = ProblemTexts.Of(exception);
        }
    }

    private ContactEntry Entry(UserListItem student)
    {
        var name = string.Join(
            ' ', new[] { student.LastName, student.FirstName, student.MiddleName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var studentId = student.Id!.Value;

        return new ContactEntry
        {
            Name = name,
            Roles = LessonTexts.DetailsOf(student.Phone, student.Email),
            OpenCommand = new Command(async () =>
            {
                if (Chosen is { } chosen)
                    await chosen(studentId, name);
            })
        };
    }
}
