using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>A lesson opened from a list of lessons, on a page of its own with its card.</summary>
public partial class LessonPage : ContentPage, IQueryAttributable
{
    /// <summary>The name of the parameter that carries the <see cref="ScheduleEntry"/> of the lesson.</summary>
    public const string EntryParameter = "entry";

    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="LessonPage"/> class.</summary>
    /// <param name="timeProvider">The clock, for how long until the lesson starts.</param>
    public LessonPage(TimeProvider timeProvider)
    {
        InitializeComponent();
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(EntryParameter, out var value) && value is ScheduleEntry entry)
        {
            Card.BindingContext = TodayCards.CardOf(
                entry, _timeProvider.GetLocalNow(), AppTexts.Get("Schedule.Lesson"), leavesPage: true);
        }
    }
}
