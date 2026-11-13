using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// Completing a driving lesson that has started: the hours it lasted, offered as its planned length, the grades of the
/// skills the student showed and a note for the student. The completion goes through the offline queue, so it can be
/// made right after the lesson without a network.
/// </summary>
public sealed class CompletionViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="CompletionRequest"/>.</summary>
    public const string RequestParameter = "request";

    private const decimal MaxHours = 24m;

    private readonly OfflineSynchronizer _synchronizer;
    private Guid _slotId;

    /// <summary>Initializes a new instance of the <see cref="CompletionViewModel"/> class.</summary>
    /// <param name="synchronizer">Sends the completion.</param>
    public CompletionViewModel(OfflineSynchronizer synchronizer)
    {
        _synchronizer = synchronizer;
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        Skills =
        [
            Skill(DrivingSkill.VehicleControl, AppTexts.Get("Skill.VehicleControl")),
            Skill(DrivingSkill.GearShifting, AppTexts.Get("Skill.GearShifting")),
            Skill(DrivingSkill.Manoeuvring, AppTexts.Get("Skill.Manoeuvring")),
            Skill(DrivingSkill.TrafficRules, AppTexts.Get("Skill.TrafficRules")),
            Skill(DrivingSkill.Observation, AppTexts.Get("Skill.Observation")),
            Skill(DrivingSkill.CityDriving, AppTexts.Get("Skill.CityDriving"))
        ];
    }

    /// <summary>Gets the student of the lesson.</summary>
    public string Student
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the day and the time of the lesson.</summary>
    public string Time
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the hours the lesson lasted, as the instructor typed them.</summary>
    public string Hours
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the skills to grade.</summary>
    public IReadOnlyList<SkillGradeEntry> Skills { get; }

    /// <summary>Gets or sets the note for the student.</summary>
    public string Note
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets what is wrong with the hours, or <see langword="null"/>.</summary>
    public string? Error
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>Gets a value indicating whether something is wrong with the hours.</summary>
    public bool HasError => Error is not null;

    /// <summary>Gets a value indicating whether the completion is being saved.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that completes the lesson.</summary>
    public Command SaveCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(RequestParameter, out var value) || value is not CompletionRequest request)
            return;

        _slotId = request.SlotId;
        Student = request.Student;
        Time = request.Time;
        Hours = request.PlannedHours.ToString("0.##", CultureInfo.CurrentCulture);
        Note = string.Empty;
        Error = null;
        foreach (var skill in Skills)
            skill.Grade = 0;
    }

    private static SkillGradeEntry Skill(DrivingSkill skill, string name) => new() { Skill = skill, Name = name };

    private static bool TryParseHours(string text, out decimal hours)
    {
        var parsed = decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out hours)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out hours);
        hours = Math.Round(hours, 2);

        return parsed && hours is > 0 and <= MaxHours;
    }

    private async Task SaveAsync()
    {
        if (!TryParseHours(Hours, out var hours))
        {
            Error = AppTexts.Get("Lesson.InvalidHours");

            return;
        }

        Error = null;
        IsBusy = true;
        try
        {
            var grades = Skills.Where(skill => skill.Grade > 0)
                .Select(skill => new DrivingSkillGrade(skill.Skill, skill.Grade))
                .ToList();
            var note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
            var result = await _synchronizer.SubmitAsync(
                new CompleteDrivingLesson(_slotId, hours, note, grades), CancellationToken.None);
            var message = result switch
            {
                { Rejected: > 0 } => AppTexts.Get("Completion.Refused"),
                { IsComplete: true } => AppTexts.Get("Completion.Saved"),
                _ => AppTexts.Get("Completion.Queued")
            };
            await Shell.Current.DisplayAlertAsync(AppTexts.Get("Lesson.CompleteTitle"), message, AppTexts.Get("Common.Ok"));
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
