using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The attendance at a theory lesson that has started: the students expected at it, each present unless marked absent
/// before, and saving the marks of the whole group at once. The marks go through the offline queue, so they can be
/// taken in a classroom without a network.
/// </summary>
public sealed class AttendanceViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The query parameter of the route that names the lesson.</summary>
    public const string LessonParameter = "lessonId";

    private readonly InstructorSchedule _schedule;
    private readonly OfflineSynchronizer _synchronizer;
    private Guid _lessonId;

    /// <summary>Initializes a new instance of the <see cref="AttendanceViewModel"/> class.</summary>
    /// <param name="schedule">The lessons of the instructor.</param>
    /// <param name="synchronizer">Sends the marks.</param>
    public AttendanceViewModel(InstructorSchedule schedule, OfflineSynchronizer synchronizer)
    {
        _schedule = schedule;
        _synchronizer = synchronizer;
        SaveCommand = new Command(async () => await SaveAsync(), () => Students.Count > 0 && !IsBusy);
    }

    /// <summary>Gets the topic of the lesson.</summary>
    public string Topic
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the day, the time and the group of the lesson.</summary>
    public string Details
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the students expected at the lesson.</summary>
    public IReadOnlyList<AttendanceEntry> Students
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    } = [];

    /// <summary>Gets that the lesson is from the device or could not be read, or <see langword="null"/>.</summary>
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

    /// <summary>Gets a value indicating whether the marks are being saved.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that saves the marks of every student shown.</summary>
    public Command SaveCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(LessonParameter, out var value) && Guid.TryParse(value?.ToString(), out var lessonId))
            _lessonId = lessonId;
    }

    /// <summary>Reads the lesson and its students.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        Cached<TheoryLessonDetails> lesson;
        try
        {
            lesson = await _schedule.ReadTheoryLessonAsync(_lessonId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Students = [];
            Notice = AppTexts.Get("Attendance.Unavailable");

            return;
        }

        var details = lesson.Value;
        Topic = details.TopicTitle ?? string.Empty;
        Details = details is { StartUtc: { } start, EndUtc: { } end }
            ? LessonTexts.DetailsOf(LessonTexts.TimeOf(start, end), details.GroupName)
            : details.GroupName ?? string.Empty;
        Students =
        [
            .. (details.Participants ?? [])
                .Where(participant => participant.ContractId is not null)
                .Select(participant => new AttendanceEntry
                {
                    ContractId = participant.ContractId!.Value,
                    Name = participant.StudentName ?? string.Empty,
                    Contract = participant.ContractNumber ?? string.Empty,
                    IsPresent = participant.IsPresent ?? true
                })
        ];
        Notice = lesson.IsFromCache ? AppTexts.Get("Attendance.Offline") : null;
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            var marks = Students.Select(student => new TheoryAttendanceMark(student.ContractId, student.IsPresent)).ToList();
            var result = await _synchronizer.SubmitAsync(new MarkTheoryAttendance(_lessonId, marks), CancellationToken.None);
            var message = result switch
            {
                { Rejected: > 0 } => AppTexts.Get("Attendance.Refused"),
                { IsComplete: true } => AppTexts.Get("Attendance.Saved"),
                _ => AppTexts.Get("Attendance.Queued")
            };
            await Shell.Current.DisplayAlertAsync(AppTexts.Get("Attendance.Title"), message, AppTexts.Get("Common.Ok"));
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
