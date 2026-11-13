using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>
/// A driving instructor completes a lesson that has started, with the hours it actually lasted and a note; the hours
/// count towards the practice of the student's contract.
/// </summary>
/// <param name="SlotId">The slot of the lesson.</param>
/// <param name="ActualHours">How long the lesson lasted, in hours, with at most two decimals.</param>
/// <param name="Note">The note of the instructor, if any.</param>
public sealed record CompleteDrivingLesson(Guid SlotId, decimal ActualHours, string? Note = null) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        // The OpenAPI document describes decimals as doubles; two decimals of hours survive the conversion.
        var command = new CompleteDrivingLessonCommand { SlotId = SlotId, ActualHours = (double)ActualHours, Note = Note };

        return api.Api.V1.DrivingSlots[SlotId].Complete.PostAsync(command, cancellationToken: cancellationToken);
    }
}
