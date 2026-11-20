using System.Globalization;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The lesson that matters most on the page of today: the one going on, or else the next one, in a highlighted card
/// with how long until it starts or ends, one main button and the rest of what the user can do with it behind a
/// button of its own.
/// </summary>
public sealed class NextLessonCard
{
    /// <summary>Gets the kind of the lesson, such as Driving.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets how long until the lesson starts, or that it is going on and for how long.</summary>
    public required string Countdown { get; init; }

    /// <summary>Gets the tone of the countdown: success while the lesson goes on, the accent before.</summary>
    public StatusTone CountdownTone { get; init; }

    /// <summary>Gets the times of the lesson, with its day when it is not today.</summary>
    public required string Time { get; init; }

    /// <summary>Gets what the lesson is: the vehicle, the student or the topic.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the icon of the title.</summary>
    public required string TitleIcon { get; init; }

    /// <summary>Gets the instructor, the place or the group.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the state of the lesson when it is not simply ahead, such as waiting to be sent.</summary>
    public string? Status { get; init; }

    /// <summary>Gets a value indicating whether the lesson has a state to show.</summary>
    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);

    /// <summary>Gets the main thing to do with the lesson, if any, such as completing it or the way to it.</summary>
    public EntryAction? Main { get; init; }

    /// <summary>Gets a value indicating whether the lesson has a main thing to do.</summary>
    public bool HasMain => Main is not null;

    /// <summary>Gets the command that offers the other things to do with the lesson, if any.</summary>
    public Command? MoreCommand { get; init; }

    /// <summary>Gets a value indicating whether there are other things to do with the lesson.</summary>
    public bool HasMore => MoreCommand is not null;
}

/// <summary>A lesson after the next one, as a row: tapping it offers what the user can do with it.</summary>
public sealed class LessonRow
{
    /// <summary>Gets the icon of the kind of the lesson.</summary>
    public required string Icon { get; init; }

    /// <summary>Gets the tone of the tile of the icon.</summary>
    public StatusTone Tone { get; init; }

    /// <summary>Gets the kind and what the lesson is, such as Theory · Road signs.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the state of the lesson, or the instructor, the place or the group.</summary>
    public required string Subtitle { get; init; }

    /// <summary>Gets when the lesson starts: the time today, or the day and the time later.</summary>
    public required string Time { get; init; }

    /// <summary>Gets the command that offers what the user can do with the lesson, if anything.</summary>
    public Command? Command { get; init; }

    /// <summary>Gets a value indicating whether tapping the row offers something to do.</summary>
    public bool HasCommand => Command is not null;
}

/// <summary>Makes the cards of the page of today out of the lessons of the user.</summary>
internal static class TodayCards
{
    // A countdown in hours and minutes up to a day before the lesson, and its day before that.
    private static readonly TimeSpan _day = TimeSpan.FromDays(1);

    /// <summary>Finds the lesson going on or else the next one; free slots and cancelled lessons do not count.</summary>
    /// <param name="lessons">The lessons of the user, the earliest first.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The lesson, or <see langword="null"/> when there is none ahead.</returns>
    public static ScheduleEntry? NextOf(IEnumerable<ScheduleEntry> lessons, DateTimeOffset now) =>
        lessons.FirstOrDefault(lesson => lesson.End > now
            && lesson.Category is ScheduleCategory.Driving or ScheduleCategory.Theory or ScheduleCategory.Exam);

    /// <summary>Makes the highlighted card of a lesson.</summary>
    /// <param name="lesson">The lesson.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The card.</returns>
    public static NextLessonCard CardOf(ScheduleEntry lesson, DateTimeOffset now)
    {
        var isGoingOn = lesson.Start <= now;
        var route = RouteOf(lesson);
        var main = lesson.Actions.FirstOrDefault(action => action.IsMain) ?? route;
        var others = lesson.Actions.Where(action => action != main).ToList();
        if (route is not null && route != main)
            others.Insert(0, route);

        return new NextLessonCard
        {
            Kind = lesson.Kind,
            Countdown = isGoingOn
                ? AppTexts.Format("Today.Ongoing", DurationOf(lesson.End - now))
                : lesson.Start - now < _day
                    ? AppTexts.Format("Today.In", DurationOf(lesson.Start - now))
                    : DayOf(lesson.Start),
            CountdownTone = isGoingOn ? StatusTone.Success : StatusTone.Accent,
            Time = TimesOf(lesson, now),
            Title = lesson.Title,
            TitleIcon = IconOf(lesson.Category),
            Details = lesson.Details,
            Status = lesson.Status,
            Main = main,
            MoreCommand = others.Count > 0 ? new Command(async () => await OfferAsync(lesson.Title, others)) : null
        };
    }

    /// <summary>Makes the row of a lesson.</summary>
    /// <param name="lesson">The lesson.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The row.</returns>
    public static LessonRow RowOf(ScheduleEntry lesson, DateTimeOffset now)
    {
        var culture = CultureInfo.CurrentCulture;
        var start = lesson.Start.ToLocalTime();
        var actions = lesson.Actions;

        return new LessonRow
        {
            Icon = IconOf(lesson.Category),
            Tone = ToneOf(lesson.Category),
            Title = LessonTexts.DetailsOf(lesson.Kind, lesson.Title),
            Subtitle = lesson.Status ?? lesson.Details,
            Time = start.Date == now.ToLocalTime().Date
                ? start.ToString("t", culture)
                : string.Create(culture, $"{start.ToString("ddd d", culture)}, {start:t}"),
            Command = actions.Count > 0 ? new Command(async () => await OfferAsync(lesson.Title, actions)) : null
        };
    }

    /// <summary>Gets the icon of a kind of lesson.</summary>
    /// <param name="category">The kind of lesson.</param>
    /// <returns>The name of the icon.</returns>
    public static string IconOf(ScheduleCategory category) => category switch
    {
        ScheduleCategory.Driving => "car",
        ScheduleCategory.Theory => "book-open",
        ScheduleCategory.Exam => "clipboard-check",
        ScheduleCategory.Free => "plus",
        _ => "circle-x"
    };

    /// <summary>Writes how long something lasts: in minutes, or in hours and minutes.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The text.</returns>
    public static string DurationOf(TimeSpan duration)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes));

        return minutes < 60
            ? AppTexts.Format("Today.Minutes", minutes)
            : AppTexts.Format("Today.HoursMinutes", minutes / 60, minutes % 60);
    }

    private static StatusTone ToneOf(ScheduleCategory category) => category switch
    {
        ScheduleCategory.Driving => StatusTone.Accent,
        ScheduleCategory.Exam => StatusTone.Warning,
        ScheduleCategory.Inactive => StatusTone.Danger,
        _ => StatusTone.Neutral
    };

    // The times of a lesson today, or its day before them later.
    private static string TimesOf(ScheduleEntry lesson, DateTimeOffset now)
    {
        var start = lesson.Start.ToLocalTime();
        var times = string.Create(CultureInfo.CurrentCulture, $"{start:t} – {lesson.End.ToLocalTime():t}");

        return start.Date == now.ToLocalTime().Date ? times : LessonTexts.DetailsOf(DayOf(lesson.Start), times);
    }

    private static string DayOf(DateTimeOffset moment) =>
        moment.ToLocalTime().ToString("ddd, d MMMM", CultureInfo.CurrentCulture);

    // The way to the ground of a driving lesson, in the maps of the device.
    private static EntryAction? RouteOf(ScheduleEntry lesson) =>
        lesson.Address is { } address && !string.IsNullOrWhiteSpace(address)
            ? new EntryAction(AppTexts.Get("Today.Route"), new Command(async () => await OpenMapAsync(address)))
            : null;

    private static async Task OpenMapAsync(string address)
    {
        try
        {
            await Map.Default.OpenAsync(new Placemark { Thoroughfare = address }, new MapLaunchOptions { Name = address });
        }
        catch (Exception exception) when (exception is FeatureNotSupportedException or InvalidOperationException)
        {
            // No maps on the device: the address can be pasted into another application.
            await Clipboard.Default.SetTextAsync(address);
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Today.Route"), AppTexts.Format("Today.AddressCopied", address), AppTexts.Get("Home.Dismiss"));
        }
    }

    // Offers what the user can do with a lesson and does what they choose.
    private static async Task OfferAsync(string title, IReadOnlyList<EntryAction> actions)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            title, AppTexts.Get("Common.Cancel"), null, [.. actions.Select(action => action.Text)]);
        if (actions.FirstOrDefault(action => action.Text == choice) is { } chosen && chosen.Command.CanExecute(null))
            chosen.Command.Execute(null);
    }
}
