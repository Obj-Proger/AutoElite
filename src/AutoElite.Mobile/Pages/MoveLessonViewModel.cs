using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The move of a booked driving lesson by a manager, as the student asked on the phone: the free slots of the next weeks
/// on a vehicle of the same category and gearbox, the earliest first. The slot chosen is booked first and the old
/// booking is cancelled after, so that the student never loses a lesson when the new slot is taken in the meantime.
/// Moving needs the network.
/// </summary>
public sealed class MoveLessonViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter with the <see cref="MoveLessonRequest"/>.</summary>
    public const string LessonParameter = "lesson";

    private readonly StudentDirectory _directory;
    private readonly TimeProvider _timeProvider;
    private MoveLessonRequest? _request;

    /// <summary>Initializes a new instance of the <see cref="MoveLessonViewModel"/> class.</summary>
    /// <param name="directory">The students of the school and the driving slots.</param>
    /// <param name="timeProvider">The clock.</param>
    public MoveLessonViewModel(StudentDirectory directory, TimeProvider timeProvider)
    {
        _directory = directory;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the student and the lesson that is moved.</summary>
    public string Summary
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the free slots the lesson can move to.</summary>
    public IReadOnlyList<LessonRow> Slots
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets what the page says when there is no free slot, or <see langword="null"/>.</summary>
    public string? EmptyText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets why the slots could not be read or the lesson could not be moved.</summary>
    public string? Message
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the slots are read or the lesson is being moved.</summary>
    public bool IsBusy
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(LessonParameter, out var value) || value is not MoveLessonRequest request)
            return;

        _request = request;
        Summary = AppTexts.Format("MoveLesson.Summary", request.StudentName, request.Summary);
        Message = null;
    }

    /// <summary>Reads the free slots.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        if (_request is not { } request)
            return;

        IsBusy = true;
        try
        {
            var slots = await _directory.FindFreeSlotsAsync(request.Category, request.Transmission, CancellationToken.None);
            var now = _timeProvider.GetUtcNow();
            Slots = [.. slots.Where(slot => slot.Id != request.SlotId).Select(slot => RowOf(slot, now)).OfType<LessonRow>()];
            EmptyText = Slots.Count == 0 ? AppTexts.Get("MoveLesson.Empty") : null;
            Message = null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private LessonRow? RowOf(DrivingSlotItem slot, DateTimeOffset now)
    {
        if (slot is not { Id: { } slotId, StartUtc: { } start, EndUtc: { } end })
            return null;

        var when = LessonTexts.TimeOf(start, end);

        return new LessonRow
        {
            Icon = "car",
            Tone = StatusTone.Accent,
            Title = slot.VehicleName ?? string.Empty,
            Subtitle = LessonTexts.DetailsOf(slot.InstructorName, slot.TrainingGroundName),
            Time = start.ToLocalTime().ToString("t", CultureInfo.CurrentCulture),
            Day = TodayCards.DayWordOf(start, now),
            Command = new Command(async () => await MoveAsync(slotId, LessonTexts.DetailsOf(when, slot.InstructorName)))
        };
    }

    private async Task MoveAsync(Guid slotId, string when)
    {
        if (IsBusy || _request is not { } request)
            return;

        var isConfirmed = await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("MoveLesson.ConfirmTitle"),
            AppTexts.Format("MoveLesson.Confirm", request.StudentName, when),
            AppTexts.Get("MoveLesson.Move"),
            AppTexts.Get("Common.Cancel"));
        if (!isConfirmed)
            return;

        IsBusy = true;
        Message = null;
        try
        {
            await _directory.BookAsync(slotId, request.ContractId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
            IsBusy = false;

            return;
        }

        try
        {
            await _directory.CancelBookingAsync(
                request.SlotId, AppTexts.Format("MoveLesson.Reason", when), CancellationToken.None);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            // The new slot is booked; the old booking stays until the manager cancels it on the card of the student.
            Message = AppTexts.Format("MoveLesson.OldKept", ProblemTexts.Of(exception));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
