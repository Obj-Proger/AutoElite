using System.Globalization;
using System.Windows.Input;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Pages;
using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The schedule as a list, which reads well at any width: each lesson as a card with the icon of its kind in a tile of
/// its color, what it is over who and where or its state, and its start over how long it lasts; a free slot is a dashed
/// card. A line marks the present moment on today and, when <see cref="ShowsFreeTime"/> is set, dashed rows between the
/// cards of a day offer its free time. Several days, such as a week, are sections under the names of the days that have
/// lessons. Tapping a lesson runs <see cref="EntryCommand"/> with it, tapping free time runs <see cref="EmptyCommand"/>
/// with its start, and tapping the name of a day runs <see cref="DayCommand"/> with the day.
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

    // The tone of the tile of a lesson by its kind: the gold for driving and free slots, a warning for an exam, danger
    // for a cancelled or missed lesson.
    private static StatusTone ToneOf(ScheduleCategory category) => category switch
    {
        ScheduleCategory.Driving or ScheduleCategory.Free => StatusTone.Accent,
        ScheduleCategory.Exam => StatusTone.Warning,
        ScheduleCategory.Inactive => StatusTone.Danger,
        _ => StatusTone.Neutral
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

    // The tile of an icon: tinted by its tone or, for free time, dashed.
    private static Border TileOf(string icon, StatusTone tone, bool isFree)
    {
        var tile = new Border
        {
            WidthRequest = 44,
            HeightRequest = 44,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            StrokeThickness = isFree ? 1 : 0,
            Stroke = ColorOf("TextMuted"),
            BackgroundColor = isFree ? Colors.Transparent : ToneColors.BackgroundOf(tone),
            VerticalOptions = LayoutOptions.Center,
            Content = new AppIcon { Name = icon, Size = 22, Color = ColorOf(ToneColors.ForegroundOf(tone)) }
        };
        if (isFree)
            tile.StrokeDashArray = [4, 3];

        return tile;
    }

    // A card of a row of the list: filled, or dashed and clear for free time.
    private static Border CardOf(View content, bool isFree)
    {
        var card = new Border
        {
            BackgroundColor = isFree ? Colors.Transparent : ColorOf("Surface"),
            Stroke = ColorOf("Border"),
            StrokeThickness = isFree ? 1 : 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Padding = new Thickness(16, 14),
            Content = content
        };
        if (isFree)
            card.StrokeDashArray = [4, 3];

        return card;
    }

    // The name of a day over its lessons, such as WEDNESDAY, 19 NOVEMBER, with a pill on today.
    private static HorizontalStackLayout HeaderOf(DateOnly day, bool isToday)
    {
        var header = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
        header.Add(new Label
        {
            Text = day.ToString("dddd, d MMMM", CultureInfo.CurrentCulture),
            FontFamily = "TextSemiBold",
            FontSize = 12,
            CharacterSpacing = 1.5,
            TextTransform = TextTransform.Uppercase,
            TextColor = isToday ? ColorOf("AccentText") : ColorOf("TextMuted"),
            VerticalOptions = LayoutOptions.Center
        });
        if (isToday)
            header.Add(new StatusPill { Text = AppTexts.Get("Schedule.Today"), Tone = StatusTone.Accent });

        return header;
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
        var list = new PageColumn { Spacing = 10 };
        if (days == 1)
        {
            AddDay(list, FirstDay, lessons, now);
        }
        else if (lessons.Count == 0)
        {
            list.Add(new EmptyState { Icon = "calendar", Text = AppTexts.Get("Schedule.Empty") });
        }
        else
        {
            for (var index = 0; index < days; index++)
            {
                var day = FirstDay.AddDays(index);
                var dayLessons = lessons.Where(lesson => DayOf(lesson.Start) == day).ToList();
                if (dayLessons.Count == 0)
                    continue;

                list.Add(Tappable(HeaderOf(day, DayOf(now) == day), DayCommand, day, day.ToString("D", CultureInfo.CurrentCulture)));
                AddRows(list, dayLessons, DayOf(now) == day ? now : null);
            }
        }

        Content = list;
    }

    // A day as cards, with the line of now on today and its free time between them when it is offered.
    private void AddDay(VerticalStackLayout list, DateOnly day, List<Placed> lessons, DateTimeOffset now)
    {
        if (lessons.Count == 0)
            list.Add(new EmptyState { Icon = "calendar", Text = AppTexts.Get("Schedule.Empty") });

        List<(DateTimeOffset Start, DateTimeOffset End)> free = ShowsFreeTime && EmptyCommand is not null && day >= DayOf(now)
            ? FreeTimeOf(day, lessons, now)
            : [];
        AddRows(list, lessons, DayOf(now) == day ? now : null, free);
    }

    // The lessons of a day in the order of time, with its free time and, on today, the line of now.
    private void AddRows(
        VerticalStackLayout list,
        List<Placed> lessons,
        DateTimeOffset? now,
        List<(DateTimeOffset Start, DateTimeOffset End)>? free = null)
    {
        var rows = new List<(DateTimeOffset At, View Row)>();
        rows.AddRange(lessons.Select(lesson => (lesson.Start, (View)RowOf(lesson))));
        rows.AddRange((free ?? []).Select(time => (time.Start, (View)FreeRowOf(time.Start, time.End))));
        if (now is { } moment && lessons.Count > 0)
            rows.Add((moment, NowLine(moment)));

        foreach (var (_, row) in rows.OrderBy(row => row.At))
            list.Add(row);
    }

    // A lesson as a card: the icon of its kind, what it is over who and where or its state, its start over its length.
    private Border RowOf(Placed lesson)
    {
        var entry = lesson.Entry;
        var isFree = entry.Category == ScheduleCategory.Free;
        var isInactive = entry.Category == ScheduleCategory.Inactive;
        var content = GridOf(14, GridLength.Auto, GridLength.Star, GridLength.Auto);
        content.Add(TileOf(TodayCards.IconOf(entry.Category), ToneOf(entry.Category), isFree), 0, 0);

        var lines = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        lines.Add(Text(TitleOf(entry), 15, isInactive ? ColorOf("TextMuted") : ColorOf("Text"), "TextSemiBold", 2));
        if (!string.IsNullOrWhiteSpace(entry.Details))
            lines.Add(Text(entry.Details, 13, ColorOf("TextMuted"), maxLines: 2));

        if (entry.Status is { } status)
            lines.Add(new StatusPill { Text = status, Tone = isInactive ? StatusTone.Danger : StatusTone.Neutral });

        content.Add(lines, 1, 0);

        var times = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        times.Add(new Label
        {
            Text = TimeOf(lesson.Start),
            FontFamily = "DisplaySemiBold",
            FontSize = 15,
            TextColor = isInactive ? ColorOf("TextMuted") : ColorOf("Text"),
            HorizontalOptions = LayoutOptions.End
        });
        var length = Text(TodayCards.DurationOf(lesson.End - lesson.Start), 12, ColorOf("TextMuted"));
        length.HorizontalOptions = LayoutOptions.End;
        times.Add(length);
        content.Add(times, 2, 0);

        return Tappable(
            CardOf(content, isFree), EntryCommand, entry, LessonTexts.DetailsOf(entry.Kind, entry.Time, entry.Title, entry.Status));
    }

    // Free time between lessons, which plans at its start when tapped.
    private Border FreeRowOf(DateTimeOffset start, DateTimeOffset end)
    {
        var text = AppTexts.Format("Schedule.Free", TimeOf(start), TimeOf(end));
        var content = GridOf(14, GridLength.Auto, GridLength.Star);
        content.Add(TileOf("plus", StatusTone.Accent, isFree: true), 0, 0);
        var label = Text(text, 15, ColorOf("TextSecondary"), "TextMedium");
        label.VerticalOptions = LayoutOptions.Center;
        content.Add(label, 1, 0);

        return Tappable(CardOf(content, isFree: true), EmptyCommand, start, text);
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
