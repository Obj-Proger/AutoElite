using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>A driving instructor records that the student did not come to a lesson that has started.</summary>
/// <param name="SlotId">The slot of the lesson.</param>
/// <param name="Note">The note of the instructor, if any.</param>
public sealed record MarkDrivingNoShow(Guid SlotId, string? Note = null) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Api.V1.DrivingSlots[SlotId].NoShow.PostAsync(
            new MarkDrivingNoShowCommand { SlotId = SlotId, Note = Note }, cancellationToken: cancellationToken);
    }
}
