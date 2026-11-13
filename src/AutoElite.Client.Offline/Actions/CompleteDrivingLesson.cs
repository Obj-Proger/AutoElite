using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>
/// A driving instructor completes a lesson that has started, with the hours it actually lasted, the grades of the
/// skills the student showed and a note; the hours count towards the practice of the student's contract and the grades
/// towards its quality score.
/// </summary>
/// <param name="SlotId">The slot of the lesson.</param>
/// <param name="ActualHours">How long the lesson lasted, in hours, with at most two decimals.</param>
/// <param name="Note">The note of the instructor, if any.</param>
/// <param name="Grades">The grades, at most one per skill; none for an ungraded lesson.</param>
public sealed record CompleteDrivingLesson(
    Guid SlotId, decimal ActualHours, string? Note = null, IReadOnlyList<DrivingSkillGrade>? Grades = null) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        // The OpenAPI document describes decimals as doubles; two decimals of hours survive the conversion.
        var command = new CompleteDrivingLessonCommand
        {
            SlotId = SlotId,
            ActualHours = (double)ActualHours,
            Note = Note,
            Grades = [.. (Grades ?? []).Select(grade => new SkillGradeInput { Skill = grade.Skill, Grade = grade.Grade })]
        };

        return api.Api.V1.DrivingSlots[SlotId].Complete.PostAsync(command, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public bool Equals(CompleteDrivingLesson? other) =>
        other is not null
        && SlotId == other.SlotId
        && ActualHours == other.ActualHours
        && Note == other.Note
        && (Grades ?? []).SequenceEqual(other.Grades ?? []);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(SlotId, ActualHours, Note);
}
