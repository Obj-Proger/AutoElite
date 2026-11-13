using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>
/// A theory instructor marks or corrects the attendance of students at a lesson that has started, usually the whole
/// group at once; the API saves all the marks or none.
/// </summary>
/// <param name="LessonId">The lesson.</param>
/// <param name="Marks">The marks, at most one per contract.</param>
public sealed record MarkTheoryAttendance(Guid LessonId, IReadOnlyList<TheoryAttendanceMark> Marks) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        var command = new MarkAttendanceCommand
        {
            LessonId = LessonId,
            Marks =
            [
                .. Marks.Select(mark => new AttendanceMark
                {
                    ContractId = mark.ContractId, IsPresent = mark.IsPresent, Comment = mark.Comment
                })
            ]
        };

        return api.Api.V1.TheoryLessons[LessonId].Attendance.PutAsync(command, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public bool Equals(MarkTheoryAttendance? other) =>
        other is not null && LessonId == other.LessonId && Marks.SequenceEqual(other.Marks);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(LessonId, Marks.Count);
}
