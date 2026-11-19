using System.Globalization;
using System.Windows.Input;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Pages;
using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The schedule as a list, which reads well at any width: a day as cards in the order of time, each with its start and
/// end, a stripe of the colour of its kind, what it is, who and where, and its state; a line marks the present moment
/// on today and, when <see cref="ShowsFreeTime"/> is set, rows between the cards offer the free time of the day. Several
/// days, such as a week, are sections under the names of their days, with a short row for each lesson. Tapping a
/// lesson runs <see cref="EntryCommand"/> with it, tapping free time runs <see cref="EmptyCommand"/> with its start, and
/// tapping the name of a day runs <see cref="DayCommand"/> with the day.
/// </summary>
public sealed class ScheduleAgenda : ContentView
{
    /// <summary>Identifies the <see cref="Entries"/> property.</summary>
    public static readonly BindableProperty EntriesProperty = BindableProperty.Create(
        nameof(Entries), typeof(IReadOnlyList<ScheduleEntry>), typeof(ScheduleAgenda), propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="FirstDay"/> property.</summary>
    public static readonly BindableProperty FirstDayProperty = BindableProperty.Create(
        nameof(FirstDay), typeof(DateOnly), typeof(ScheduleAgenda), DateOnly.MinValue, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="DayCount"/> property.</summary>
    public static readonly BindableProperty DayCountProperty = BindableProperty.Create(
        nameof(DayCount), typeof(int), typeof(ScheduleAgenda), 1, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="ShowsFreeTime"/> property.</summary>
    public static readonly BindableProperty ShowsFreeTimeProperty = BindableProperty.Create(
        nameof(ShowsFreeTime), typeof(bool), typeof(ScheduleAgenda), false, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="EntryCommand"/> property.</summary>
    public static readonly BindableProperty EntryCommandProperty = BindableProperty.Create(
        nameof(EntryCommand), typeof(ICommand), typeof(ScheduleAgenda), propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="EmptyCommand"/> property.</summary>
    public static readonly BindableProperty EmptyCommandProperty = BindableProperty.Create(
        nameof(EmptyCommand), typeof(ICommand), typeof(ScheduleAgenda), propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="DayCommand"/> property.</summary>
    public static readonly BindableProperty DayCommandProperty = BindableProperty.Create(
        nameof(DayCommand), typeof(ICommand), typeof(ScheduleAgenda), propertyChanged: OnLayoutChanged);

    // The working day within which free time is offered, and the shortest free time worth a row.
    private const int FirstWorkingHour = 8;
    private const int LastWorkingHour = 21;
    private static readonly TimeSpan _shortestFreeTime = TimeSpan.FromMinutes(30);

    /// <summary>Initializes a new instance of the <see cref="ScheduleAgenda"/> class.</summary>
    public ScheduleAgenda()
    {
        Loaded += (_, _) => AppPalette.Changed += OnPaletteChanged;
        Unloaded += (_, _) => AppPalette.Changed -= OnPaletteChanged;
    }

    /// <summary>Gets or sets the lessons; those outside the days shown are left out.</summary>
    public IReadOnlyList<ScheduleEntry>? Entries
    {
        get => (IReadOnlyList<ScheduleEntry>?)GetValue(EntriesProperty);
        set => SetValue(EntriesProperty, value);
    }

    /// <summary>Gets or sets the first day shown.</summary>
    public DateOnly FirstDay
    {
        get => (DateOnly)GetValue(FirstDayProperty);
        set => SetValue(FirstDayProperty, value);
    }

    /// <summary>Gets or sets the number of days shown: 1 for a day, which shows cards, 7 for a week, which shows rows.</summary>
    public int DayCount
    {
        get => (int)GetValue(DayCountProperty);
        set => SetValue(DayCountProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether a day offers its free time, ahead of now, to plan in.</summary>
    public bool ShowsFreeTime
    {
        get => (bool)GetValue(ShowsFreeTimeProperty);
        set => SetValue(ShowsFreeTimeProperty, value);
    }

    /// <summary>Gets or sets the command that a tapped lesson runs, with the lesson as its parameter.</summary>
    public ICommand? EntryCommand
    {
        get => (ICommand?)GetValue(EntryCommandProperty);
        set => SetValue(EntryCommandProperty, value);
    }

    /// <summary>Gets or sets the command that tapped free time runs, with its start as a <see cref="DateTimeOffset"/>.</summary>
    public ICommand? EmptyCommand
    {
        get => (ICommand?)GetValue(EmptyCommandProperty);
        set => SetValue(EmptyCommandProperty, value);
    }

    /// <summary>Gets or sets the command that the tapped name of a day runs, with the day as a <see cref="DateOnly"/>.</summary>
    public ICommand? DayCommand
    {
        get => (ICommand?)GetValue(DayCommandProperty);
        set => SetValue(DayCommandProperty, value);
    }

    private static void OnLayoutChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ScheduleAgenda)bindable).Rebuild();

    private static Color ColorOf(string key) => AppPalette.ColorOf(key);

    private static DateOnly DayOf(DateTimeOffset moment) => DateOnly.FromDateTime(moment.DateTime);

    private static string TimeOf(DateTimeOffset moment) => moment.ToString("t", CultureInfo.CurrentCulture);

    // What a lesson is: its kind and its title, such as "Driving · Lada Vesta", or the title alone when it names the
    // kind already, such as "Practice exam".
    private static string TitleOf(ScheduleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Title))
            return entry.Kind;

        return entry.Title.Contains(entry.Kind, StringComparison.CurrentCultureIgnoreCase)
            ? entry.Title
            : string.Create(CultureInfo.CurrentCulture, $"{entry.Kind} · {entry.Title}");
    }

    // The colour of the stripe and of the text of a lesson by its kind; a cancelled lesson and a missed one are muted.
    private static (Color Stripe, Color Text) ColorsOf(ScheduleCategory category) => category switch
    {
        ScheduleCategory.Driving => (ColorOf("Accent"), ColorOf("Text")),
        ScheduleCategory.Theory => (ColorOf("TextSecondary"), ColorOf("Text")),
        ScheduleCategory.Free => (ColorOf("Accent"), ColorOf("AccentText")),
        ScheduleCategory.Exam => (ColorOf("Warning"), ColorOf("Warning")),
        _ => (ColorOf("Border"), ColorOf("TextMuted"))
    };

    private static Label Text(string text, double size, Color color, string font = "TextRegular", int maxLines = 1) => new()
    {
        Text = text,
        FontFamily = font,
        FontSize = size,
        TextColor = color,
        MaxLines = maxLines,
        LineBreakMode = LineBreakMode.TailTruncation
    };

    // A grid of one row with columns of the given widths.
    private static Grid GridOf(double spacing, params GridLength[] widths)
    {
        var grid = new Grid { ColumnSpacing = spacing };
        foreach (var width in widths)
            grid.ColumnDefinitions.Add(new ColumnDefinition(width));

        return grid;
    }

    // The line of the present moment, with its time.
    private static Grid NowLine(DateTimeOffset now)
    {
        var danger = ColorOf("Danger");
        var line = GridOf(8, GridLength.Auto, GridLength.Star);
        line.Add(Text(AppTexts.Format("Schedule.Now", TimeOf(now)), 12, danger, "TextMedium"), 0, 0);
        line.Add(new BoxView { Color = danger, HeightRequest = 2, VerticalOptions = LayoutOptions.Center }, 1, 0);

        return line;
    }

    // The free stretches of a day between its lessons, within the working day and ahead of now.
    private static List<(DateTimeOffset Start, DateTimeOffset End)> FreeTimeOf(
        DateOnly day, IReadOnlyList<Placed> lessons, DateTimeOffset now)
    {
        var dayStart = new DateTimeOffset(day.ToDateTime(new TimeOnly(FirstWorkingHour, 0)));
        var dayEnd = new DateTimeOffset(day.ToDateTime(new TimeOnly(LastWorkingHour, 0)));

        // From the next half hour when the day has begun, so that free time is never in the past.
        var halfHour = TimeSpan.FromMinutes(30);
        var nextHalfHour = new DateTimeOffset(now.Ticks - (now.Ticks % halfHour.Ticks), now.Offset) + halfHour;
        var cursor = nextHalfHour > dayStart ? nextHalfHour : dayStart;
        var free = new List<(DateTimeOffset, DateTimeOffset)>();
        var busy = lessons.Where(lesson => lesson.Entry.Category != ScheduleCategory.Inactive).OrderBy(lesson => lesson.Start);
        foreach (var lesson in busy)
        {
            if (lesson.Start - cursor >= _shortestFreeTime && lesson.Start <= dayEnd)
                free.Add((cursor, lesson.Start));

            if (lesson.End > cursor)
                cursor = lesson.End;
        }

        if (dayEnd - cursor >= _shortestFreeTime)
            free.Add((cursor, dayEnd));

        return free;
    }

    // The colours are taken when the views are made, so a new theme makes them again.
    private void OnPaletteChanged(object? sender, EventArgs args) => Dispatcher.Dispatch(Rebuild);

    private void Rebuild()
    {
        var days = Math.Max(1, DayCount);
        var lastDay = FirstDay.AddDays(days);
        List<Placed> lessons =
        [
            .. (Entries ?? []).Select(entry => new Placed(entry, entry.Start.ToLocalTime(), entry.End.ToLocalTime()))
                .Where(lesson => DayOf(lesson.Start) >= FirstDay && DayOf(lesson.Start) < lastDay)
                .OrderBy(lesson => lesson.Start)
        ];
        var now = DateTimeOffset.Now;
        var list = new VerticalStackLayout { Spacing = days == 1 ? 10 : 18 };
        for (var index = 0; index < days; index++)
        {
            var day = FirstDay.AddDays(index);
            var dayLessons = lessons.Where(lesson => DayOf(lesson.Start) == day).ToList();
            if (days == 1)
                AddDay(list, day, dayLessons, now);
            else
                list.Add(SectionOf(day, dayLessons, now));
        }

        Content = list;
    }

    // A day as cards, with the line of now on today and its free time between them when it is offered.
    private void AddDay(VerticalStackLayout list, DateOnly day, List<Placed> lessons, DateTimeOffset now)
    {
        var rows = new List<(DateTimeOffset At, View Row)>();
        rows.AddRange(lessons.Select(lesson => (lesson.Start, (View)CardOf(lesson))));
        if (ShowsFreeTime && EmptyCommand is not null && day >= DayOf(now))
            rows.AddRange(FreeTimeOf(day, lessons, now).Select(free => (free.Start, (View)FreeRowOf(free.Start, free.End))));

        if (DayOf(now) == day)
            rows.Add((now, NowLine(now)));

        if (lessons.Count == 0)
            rows.Insert(0, (DateTimeOffset.MinValue, (View)Text(AppTexts.Get("Schedule.Empty"), 15, ColorOf("TextSecondary"))));

        foreach (var (_, row) in rows.OrderBy(row => row.At))
            list.Add(row);
    }

    // A lesson as a card: its start over its end, the stripe of its kind, what it is, who and where, and its state.
    private Border CardOf(Placed lesson)
    {
        var entry = lesson.Entry;
        var (stripe, text) = ColorsOf(entry.Category);
        var content = GridOf(12, new GridLength(52), new GridLength(4), GridLength.Star);

        var times = new VerticalStackLayout { Spacing = 2 };
        times.Add(Text(TimeOf(lesson.Start), 16, ColorOf("Text"), "TextSemiBold"));
        times.Add(Text(TimeOf(lesson.End), 13, ColorOf("TextMuted")));
        content.Add(times, 0, 0);
        content.Add(new BoxView { Color = stripe, CornerRadius = 2, VerticalOptions = LayoutOptions.Fill }, 1, 0);

        var lines = new VerticalStackLayout { Spacing = 3 };
        lines.Add(Text(TitleOf(entry), 16, text, "TextSemiBold", 2));
        if (!string.IsNullOrWhiteSpace(entry.Details))
            lines.Add(Text(entry.Details, 14, ColorOf("TextSecondary"), maxLines: 2));

        if (entry.Status is { } status)
        {
            var statusColor = entry.Category == ScheduleCategory.Inactive ? ColorOf("TextMuted") : stripe;
            lines.Add(Text(status, 13, statusColor, "TextMedium", 2));
        }

        content.Add(lines, 2, 0);

        var card = new Border
        {
            BackgroundColor = entry.Category == ScheduleCategory.Free ? Colors.Transparent : ColorOf("Surface"),
            Stroke = entry.Category == ScheduleCategory.Free ? ColorOf("Accent") : ColorOf("Border"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(14, 12),
            Content = content
        };
        if (entry.Category == ScheduleCategory.Free)
            card.StrokeDashArray = [4, 3];

        return Tappable(card, EntryCommand, entry, LessonTexts.DetailsOf(entry.Kind, entry.Time, entry.Title, entry.Status));
    }

    // Free time between lessons, which plans at its start when tapped.
    private Border FreeRowOf(DateTimeOffset start, DateTimeOffset end)
    {
        var text = AppTexts.Format("Schedule.Free", TimeOf(start), TimeOf(end));
        var row = GridOf(8, GridLength.Star, GridLength.Auto);
        row.Add(Text(text, 14, ColorOf("TextSecondary")), 0, 0);
        row.Add(Text("+", 18, ColorOf("AccentText"), "TextSemiBold"), 1, 0);
        var border = new Border
        {
            BackgroundColor = Colors.Transparent,
            Stroke = ColorOf("Border"),
            StrokeThickness = 1,
            StrokeDashArray = [4, 3],
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(14, 10),
            Content = row
        };

        return Tappable(border, EmptyCommand, start, text);
    }

    // A day of several as a section: its name, which shows the day when tapped, then a short row for each lesson.
    private VerticalStackLayout SectionOf(DateOnly day, List<Placed> lessons, DateTimeOffset now)
    {
        var culture = CultureInfo.CurrentCulture;
        var isToday = DayOf(now) == day;
        var name = day.ToString("dddd, d MMMM", culture);
        var header = new HorizontalStackLayout { Spacing = 8 };
        var title = string.Concat(name[..1].ToUpper(culture), name[1..]);
        header.Add(Text(title, 17, isToday ? ColorOf("AccentText") : ColorOf("Text"), "TextSemiBold"));
        if (isToday)
            header.Add(Text(AppTexts.Get("Schedule.Today"), 13, ColorOf("AccentText"), "TextMedium"));

        var section = new VerticalStackLayout { Spacing = 6 };
        section.Add(Tappable(header, DayCommand, day, title));
        var rows = new List<(DateTimeOffset At, View Row)>();
        rows.AddRange(lessons.Select(lesson => (lesson.Start, (View)RowOf(lesson))));
        if (isToday && lessons.Count > 0)
            rows.Add((now, NowLine(now)));

        if (lessons.Count == 0)
            rows.Add((DateTimeOffset.MinValue, (View)Text(AppTexts.Get("Schedule.Empty"), 14, ColorOf("TextMuted"))));

        foreach (var (_, row) in rows.OrderBy(row => row.At))
            section.Add(row);

        return section;
    }

    // A lesson of a week as a row: its times, the stripe of its kind, what it is and its state or details.
    private Grid RowOf(Placed lesson)
    {
        var entry = lesson.Entry;
        var (stripe, text) = ColorsOf(entry.Category);
        var row = GridOf(10, new GridLength(96), new GridLength(3), GridLength.Star);
        row.Padding = new Thickness(0, 4);
        row.BackgroundColor = Colors.Transparent;
        var times = string.Create(CultureInfo.CurrentCulture, $"{TimeOf(lesson.Start)}–{TimeOf(lesson.End)}");
        row.Add(Text(times, 14, ColorOf("TextSecondary")), 0, 0);
        row.Add(new BoxView { Color = stripe, CornerRadius = 1.5, VerticalOptions = LayoutOptions.Fill }, 1, 0);

        var lines = new VerticalStackLayout { Spacing = 1 };
        lines.Add(Text(TitleOf(entry), 15, text, "TextMedium"));
        if ((entry.Status ?? entry.Details) is { Length: > 0 } detail)
            lines.Add(Text(detail, 13, ColorOf("TextMuted")));

        row.Add(lines, 2, 0);

        return Tappable(row, EntryCommand, entry, LessonTexts.DetailsOf(entry.Kind, entry.Time, entry.Title, entry.Status));
    }

    private static T Tappable<T>(T view, ICommand? command, object parameter, string description)
        where T : View
    {
        SemanticProperties.SetDescription(view, description);
        if (command is not null)
            view.GestureRecognizers.Add(new TapGestureRecognizer { Command = command, CommandParameter = parameter });

        return view;
    }

    // A lesson with its times on the device.
    private readonly record struct Placed(ScheduleEntry Entry, DateTimeOffset Start, DateTimeOffset End);
}
