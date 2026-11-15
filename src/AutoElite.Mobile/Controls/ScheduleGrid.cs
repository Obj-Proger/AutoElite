using System.Globalization;
using System.Windows.Input;
using AutoElite.Mobile.Pages;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The schedule as a grid of hours: one column for a day or seven equal ones for a week under the names of their days,
/// with the hours down the side and each lesson a block from its start to its end, coloured by its kind; lessons at the
/// same time share the width of their column. A line marks the present moment on today. Tapping a block runs
/// <see cref="EntryCommand"/> with its lesson; tapping free time runs <see cref="EmptyCommand"/> with its start, to the
/// half hour, in the time zone of the device; tapping the name of a day runs <see cref="DayCommand"/> with the day.
/// </summary>
public sealed class ScheduleGrid : ContentView
{
    /// <summary>The height of an hour.</summary>
    public const double HourHeight = 64;

    /// <summary>Identifies the <see cref="Entries"/> property.</summary>
    public static readonly BindableProperty EntriesProperty = BindableProperty.Create(
        nameof(Entries), typeof(IReadOnlyList<ScheduleEntry>), typeof(ScheduleGrid), propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="FirstDay"/> property.</summary>
    public static readonly BindableProperty FirstDayProperty = BindableProperty.Create(
        nameof(FirstDay), typeof(DateOnly), typeof(ScheduleGrid), DateOnly.MinValue, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="DayCount"/> property.</summary>
    public static readonly BindableProperty DayCountProperty = BindableProperty.Create(
        nameof(DayCount), typeof(int), typeof(ScheduleGrid), 1, propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="EntryCommand"/> property.</summary>
    public static readonly BindableProperty EntryCommandProperty = BindableProperty.Create(
        nameof(EntryCommand), typeof(ICommand), typeof(ScheduleGrid), propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="EmptyCommand"/> property.</summary>
    public static readonly BindableProperty EmptyCommandProperty = BindableProperty.Create(
        nameof(EmptyCommand), typeof(ICommand), typeof(ScheduleGrid), propertyChanged: OnLayoutChanged);

    /// <summary>Identifies the <see cref="DayCommand"/> property.</summary>
    public static readonly BindableProperty DayCommandProperty = BindableProperty.Create(
        nameof(DayCommand), typeof(ICommand), typeof(ScheduleGrid), propertyChanged: OnLayoutChanged);

    // The hours shown at least, widened to the lessons of the days shown.
    private const int EarliestHour = 8;
    private const int LatestHour = 21;
    private const double HoursWidth = 44;
    private const double MinimumBlockHeight = 36;
    private const double ColumnSpacing = 4;

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

    /// <summary>Gets or sets the number of days shown: 1 for a day, 7 for a week.</summary>
    public int DayCount
    {
        get => (int)GetValue(DayCountProperty);
        set => SetValue(DayCountProperty, value);
    }

    /// <summary>Gets or sets the command that a tapped lesson runs, with the lesson as its parameter.</summary>
    public ICommand? EntryCommand
    {
        get => (ICommand?)GetValue(EntryCommandProperty);
        set => SetValue(EntryCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that tapped free time runs, with its start as a <see cref="DateTimeOffset"/>; free time
    /// is not tappable when it is <see langword="null"/>.
    /// </summary>
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
        ((ScheduleGrid)bindable).Rebuild();

    private static Color ColorOf(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : Colors.Gray;

    private static DateOnly DayOf(DateTimeOffset moment) => DateOnly.FromDateTime(moment.DateTime);

    // Gives each lesson of a day its lane: lessons that overlap, directly or through others, share the width equally.
    private static List<(Placed Lesson, int Lane, int Lanes)> LanesOf(IEnumerable<Placed> lessons)
    {
        var result = new List<(Placed, int, int)>();
        var cluster = new List<(Placed Lesson, int Lane)>();
        var laneEnds = new List<DateTimeOffset>();
        var clusterEnd = DateTimeOffset.MinValue;

        void Close()
        {
            result.AddRange(cluster.Select(placed => (placed.Lesson, placed.Lane, laneEnds.Count)));
            cluster.Clear();
            laneEnds.Clear();
        }

        foreach (var lesson in lessons.OrderBy(lesson => lesson.Start))
        {
            if (cluster.Count > 0 && lesson.Start >= clusterEnd)
                Close();

            var lane = laneEnds.FindIndex(end => end <= lesson.Start);
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(lesson.End);
            }
            else
            {
                laneEnds[lane] = lesson.End;
            }

            cluster.Add((lesson, lane));
            if (lesson.End > clusterEnd)
                clusterEnd = lesson.End;
        }

        Close();

        return result;
    }

    private void Rebuild()
    {
        var days = Math.Max(1, DayCount);
        var lastDay = FirstDay.AddDays(days);
        List<Placed> lessons =
        [
            .. (Entries ?? []).Select(entry => new Placed(entry, entry.Start.ToLocalTime(), entry.End.ToLocalTime()))
                .Where(lesson => DayOf(lesson.Start) >= FirstDay && DayOf(lesson.Start) < lastDay)
        ];
        var firstHour = Math.Min(EarliestHour, lessons.Count == 0 ? EarliestHour : lessons.Min(lesson => lesson.Start.Hour));
        var lastHour = Math.Max(
            LatestHour,
            lessons.Count == 0 ? LatestHour : lessons.Max(lesson => lesson.End.Hour + (lesson.End.Minute > 0 ? 1 : 0)));
        lastHour = Math.Min(24, Math.Max(lastHour, firstHour + 1));
        var height = (lastHour - firstHour) * HourHeight;

        var grid = new Grid { ColumnSpacing = ColumnSpacing, HeightRequest = height };
        AddColumns(grid, days);

        var hours = new AbsoluteLayout();
        for (var hour = firstHour; hour < lastHour; hour++)
        {
            var label = new Label
            {
                Text = new TimeOnly(hour, 0).ToString("t", CultureInfo.CurrentCulture),
                FontSize = 11,
                TextColor = ColorOf("TextMuted")
            };
            hours.Add(label);
            AbsoluteLayout.SetLayoutBounds(label, new Rect(0, Math.Max(0, ((hour - firstHour) * HourHeight) - 7), 1, 16));
            AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.WidthProportional);
        }

        grid.Add(hours, 0, 0);

        var now = DateTimeOffset.Now;
        for (var index = 0; index < days; index++)
        {
            var day = FirstDay.AddDays(index);
            var column = new AbsoluteLayout();
            for (var hour = 0; hour <= lastHour - firstHour; hour++)
                AddLine(column, hour * HourHeight, ColorOf("Border"), 1);

            if (DayOf(now) == day && now.Hour >= firstHour && now.Hour < lastHour)
                AddLine(column, (now.TimeOfDay.TotalHours - firstHour) * HourHeight, ColorOf("Danger"), 2);

            foreach (var (lesson, lane, lanes) in LanesOf(lessons.Where(lesson => DayOf(lesson.Start) == day)))
            {
                var top = (lesson.Start.TimeOfDay.TotalHours - firstHour) * HourHeight;
                var blockHeight = Math.Max(MinimumBlockHeight, (lesson.End - lesson.Start).TotalHours * HourHeight - 2);
                var block = BlockOf(lesson, days == 1);
                column.Add(block);

                // A proportional X places the block at X of the width left beside it, which puts the lanes side by side.
                AbsoluteLayout.SetLayoutBounds(
                    block, new Rect(lanes == 1 ? 0 : (double)lane / (lanes - 1), top + 1, 1.0 / lanes, blockHeight));
                AbsoluteLayout.SetLayoutFlags(block, AbsoluteLayoutFlags.XProportional | AbsoluteLayoutFlags.WidthProportional);
            }

            if (EmptyCommand is { } emptyCommand)
                AddFreeTimeTap(column, day, firstHour, [.. lessons.Where(lesson => DayOf(lesson.Start) == day)], emptyCommand);

            grid.Add(column, index + 1, 0);
        }

        if (days == 1)
        {
            Content = grid;

            return;
        }

        var week = new VerticalStackLayout { Spacing = 12 };
        week.Add(HeaderOf(days, now));
        week.Add(grid);
        Content = week;
    }

    // The column of the hours and one equal column for each day, the same in the header and in the grid.
    private static void AddColumns(Grid grid, int days)
    {
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(HoursWidth)));
        for (var day = 0; day < days; day++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
    }

    // The names and numbers of the days over their columns, today in the accent colour.
    private Grid HeaderOf(int days, DateTimeOffset now)
    {
        var culture = CultureInfo.CurrentCulture;
        var header = new Grid { ColumnSpacing = ColumnSpacing };
        AddColumns(header, days);
        for (var index = 0; index < days; index++)
        {
            var day = FirstDay.AddDays(index);
            var color = DayOf(now) == day ? ColorOf("Accent") : ColorOf("Text");
            var cell = new VerticalStackLayout { Spacing = 0, BackgroundColor = Colors.Transparent };
            cell.Add(new Label
            {
                Text = day.ToString("ddd", culture),
                FontSize = 12,
                TextColor = DayOf(now) == day ? color : ColorOf("TextMuted"),
                HorizontalTextAlignment = TextAlignment.Center
            });
            cell.Add(new Label
            {
                Text = day.Day.ToString(culture),
                FontFamily = "TextSemiBold",
                FontSize = 18,
                TextColor = color,
                HorizontalTextAlignment = TextAlignment.Center
            });
            SemanticProperties.SetDescription(cell, day.ToString("D", culture));
            if (DayCommand is { } command)
                cell.GestureRecognizers.Add(new TapGestureRecognizer { Command = command, CommandParameter = day });

            header.Add(cell, index + 1, 0);
        }

        return header;
    }

    // Runs the command with the half hour tapped, unless a lesson is there; the column needs a background to be tapped.
    private static void AddFreeTimeTap(
        AbsoluteLayout column, DateOnly day, int firstHour, IReadOnlyList<Placed> dayLessons, ICommand command)
    {
        column.BackgroundColor = Colors.Transparent;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, args) =>
        {
            if (args.GetPosition(column) is not { } point || point.Y < 0)
                return;

            var halfHours = (int)(point.Y / (HourHeight / 2));
            var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue).AddHours(firstHour).AddMinutes(halfHours * 30));
            if (dayLessons.Any(lesson => lesson.Start <= start && start < lesson.End) || !command.CanExecute(start))
                return;

            command.Execute(start);
        };
        column.GestureRecognizers.Add(tap);
    }

    private static void AddLine(AbsoluteLayout column, double top, Color color, double thickness)
    {
        var line = new BoxView { Color = color };
        column.Add(line);
        AbsoluteLayout.SetLayoutBounds(line, new Rect(0, top, 1, thickness));
        AbsoluteLayout.SetLayoutFlags(line, AbsoluteLayoutFlags.WidthProportional);
    }

    // A lesson as a block: its time and title, and on a day also its details and state.
    private Border BlockOf(Placed lesson, bool isDay)
    {
        var entry = lesson.Entry;
        var (background, stroke, text) = entry.Category switch
        {
            ScheduleCategory.Driving => (ColorOf("Accent"), ColorOf("Accent"), ColorOf("OnAccent")),
            ScheduleCategory.Theory => (ColorOf("SurfaceRaised"), ColorOf("TextSecondary"), ColorOf("Text")),
            ScheduleCategory.Free => (Colors.Transparent, ColorOf("Accent"), ColorOf("Accent")),
            ScheduleCategory.Exam => (ColorOf("Surface"), ColorOf("Warning"), ColorOf("Warning")),
            _ => (ColorOf("Surface"), ColorOf("Border"), ColorOf("TextMuted"))
        };

        var culture = CultureInfo.CurrentCulture;
        var lines = new VerticalStackLayout { Spacing = 1 };
        lines.Add(new Label
        {
            Text = string.Create(culture, $"{lesson.Start:t}–{lesson.End:t}"),
            FontSize = 11,
            TextColor = text,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        lines.Add(new Label
        {
            Text = entry.Title,
            FontFamily = "TextSemiBold",
            FontSize = isDay ? 13 : 12,
            TextColor = text,
            MaxLines = 2,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        if (isDay)
        {
            foreach (var detail in new[] { entry.Details, entry.Status }.Where(detail => !string.IsNullOrWhiteSpace(detail)))
                lines.Add(new Label { Text = detail, FontSize = 11, TextColor = text, LineBreakMode = LineBreakMode.TailTruncation });
        }

        var block = new Border
        {
            BackgroundColor = background,
            Stroke = stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(6, 4),
            Content = lines
        };
        if (entry.Category == ScheduleCategory.Free)
            block.StrokeDashArray = [4, 3];

        block.GestureRecognizers.Add(new TapGestureRecognizer { Command = EntryCommand, CommandParameter = entry });
        SemanticProperties.SetDescription(block, LessonTexts.DetailsOf(entry.Kind, entry.Time, entry.Title, entry.Status));

        return block;
    }

    // A lesson with its times on the device.
    private readonly record struct Placed(ScheduleEntry Entry, DateTimeOffset Start, DateTimeOffset End);
}
