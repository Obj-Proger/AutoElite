using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A theory lesson that a theory instructor plans: a new lesson of one of their groups on a topic of its course, or a
/// scheduled lesson moved to another day, start or length. The students of the group are notified; planning needs the
/// network.
/// </summary>
public sealed class PlanTheoryViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="TheoryLessonRequest"/>.</summary>
    public const string LessonParameter = "lesson";

    // The lengths a lesson can have, in minutes; two academic hours are usual.
    private static readonly int[] _lengths = [45, 90, 135, 180, 240];
    private const int UsualLength = 90;

    private readonly TheoryPlans _plans;
    private readonly TimeProvider _timeProvider;
    private StartChoices? _starts;
    private IReadOnlyList<StudyGroupListItem> _groups = [];
    private IReadOnlyList<Guid> _topicIds = [];
    private Guid? _lessonId;

    /// <summary>Initializes a new instance of the <see cref="PlanTheoryViewModel"/> class.</summary>
    /// <param name="plans">The theory lessons of the instructor.</param>
    /// <param name="timeProvider">The clock.</param>
    public PlanTheoryViewModel(TheoryPlans plans, TimeProvider timeProvider)
    {
        _plans = plans;
        _timeProvider = timeProvider;
        Lengths = [.. _lengths.Select(LengthName)];
        LengthIndex = Array.IndexOf(_lengths, UsualLength);
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        Choose(_timeProvider.GetLocalNow().AddHours(1));
    }

    /// <summary>Gets the title of the page: a new lesson, or moving one.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = AppTexts.Get("PlanTheory.NewTitle");

    /// <summary>Gets a value indicating whether a new lesson is planned, with its group and topic, rather than one moved.</summary>
    public bool IsNew
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsMove));
        }
    } = true;

    /// <summary>Gets a value indicating whether a scheduled lesson is moved.</summary>
    public bool IsMove => !IsNew;

    /// <summary>Gets the topic and the group of the lesson moved.</summary>
    public string Moved
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the active groups of the instructor.</summary>
    public IReadOnlyList<string> Groups
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the group, by its position; choosing one reads the topics of its course.</summary>
    public int GroupIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                _ = LoadTopicsAsync();
        }
    } = -1;

    /// <summary>Gets the topics of the course of the group, in their order.</summary>
    public IReadOnlyList<string> Topics
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the topic, by its position.</summary>
    public int TopicIndex
    {
        get;
        set => SetProperty(ref field, value);
    } = -1;

    /// <summary>Gets the days the lesson can be on, from today.</summary>
    public IReadOnlyList<string> Days
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the day, by its position.</summary>
    public int DayIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the times the lesson can start at, every half hour.</summary>
    public IReadOnlyList<string> Times
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the start, by its position.</summary>
    public int TimeIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the lengths the lesson can have.</summary>
    public IReadOnlyList<string> Lengths { get; }

    /// <summary>Gets or sets the length, by its position.</summary>
    public int LengthIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets why the lesson could not be planned, or that the lists are from the device, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets a value indicating whether the lesson is being saved.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that schedules or moves the lesson.</summary>
    public Command SaveCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(LessonParameter, out var value) || value is not TheoryLessonRequest request)
            return;

        Choose(request.Start);
        _lessonId = request.LessonId;
        IsNew = request.LessonId is null;
        Title = IsNew ? AppTexts.Get("PlanTheory.NewTitle") : AppTexts.Get("PlanTheory.MoveTitle");
        Moved = request.Title ?? string.Empty;
        if (request.End is { } end)
        {
            var minutes = (int)(end - request.Start).TotalMinutes;
            var nearest = _lengths.OrderBy(length => Math.Abs(length - minutes)).First();
            LengthIndex = Array.IndexOf(_lengths, nearest);
        }
    }

    /// <summary>Reads the groups of the instructor, for a new lesson.</summary>
    /// <returns>A task that completes when they can be chosen.</returns>
    public async Task LoadAsync()
    {
        if (!IsNew || _groups.Count > 0)
            return;

        try
        {
            var groups = await _plans.ReadGroupsAsync(CancellationToken.None);
            _groups = [.. groups.Value.Where(group => group.Id is not null && group.CourseId is not null)];
            Groups = [.. _groups.Select(group => group.Name ?? string.Empty)];
            Message = _groups.Count == 0
                ? AppTexts.Get("PlanTheory.NoGroups")
                : groups.IsFromCache ? AppTexts.Get("PlanTheory.Offline") : null;
            GroupIndex = _groups.Count == 1 ? 0 : -1;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Message = ProblemTexts.Of(exception);
        }
    }

    private static string LengthName(int minutes) =>
        AppTexts.Format("PlanTheory.Minutes", minutes.ToString(CultureInfo.CurrentCulture));

    private void Choose(DateTimeOffset start)
    {
        _starts = StartChoices.Around(start, DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime));
        Days = _starts.DayNames;
        DayIndex = _starts.DayIndex;
        Times = _starts.TimeNames;
        TimeIndex = _starts.TimeIndex;
    }

    // Reads the topics of the course of the group chosen.
    private async Task LoadTopicsAsync()
    {
        _topicIds = [];
        Topics = [];
        TopicIndex = -1;
        if (GroupIndex < 0 || GroupIndex >= _groups.Count)
            return;

        try
        {
            var course = await _plans.ReadCourseAsync(_groups[GroupIndex].CourseId!.Value, CancellationToken.None);
            List<CourseTopicItem> topics =
                [.. (course.Value.Topics ?? []).Where(topic => topic.Id is not null).OrderBy(topic => topic.Position)];
            _topicIds = [.. topics.Select(topic => topic.Id!.Value)];
            Topics = [.. topics.Select(topic => string.Create(CultureInfo.CurrentCulture, $"{topic.Position}. {topic.Title}"))];
            if (topics.Count == 0)
                Message = AppTexts.Get("PlanTheory.NoTopics");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Message = ProblemTexts.Of(exception);
        }
    }

    private async Task SaveAsync()
    {
        var start = _starts!.StartAt(DayIndex, TimeIndex);
        var end = start.AddMinutes(_lengths[Math.Max(0, LengthIndex)]);
        if (start <= _timeProvider.GetUtcNow())
        {
            Message = AppTexts.Get("PlanTheory.InThePast");

            return;
        }

        if (IsNew && (GroupIndex < 0 || GroupIndex >= _groups.Count || TopicIndex < 0 || TopicIndex >= _topicIds.Count))
        {
            Message = AppTexts.Get("PlanTheory.ChooseGroupAndTopic");

            return;
        }

        IsBusy = true;
        Message = null;
        try
        {
            if (_lessonId is { } lessonId)
                await _plans.RescheduleAsync(lessonId, start, end, CancellationToken.None);
            else
                await _plans.ScheduleAsync(_groups[GroupIndex].Id!.Value, _topicIds[TopicIndex], start, end, CancellationToken.None);

            await Shell.Current.GoToAsync("..");
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
}
