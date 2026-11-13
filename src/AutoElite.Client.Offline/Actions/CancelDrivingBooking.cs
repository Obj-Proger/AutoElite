using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>A student or the instructor cancels the booking of a practice slot before the lesson starts.</summary>
/// <param name="SlotId">The slot.</param>
/// <param name="Reason">Why the booking is cancelled.</param>
public sealed record CancelDrivingBooking(Guid SlotId, string Reason) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Api.V1.DrivingSlots[SlotId].CancelBooking.PostAsync(
            new CancelDrivingBookingCommand { SlotId = SlotId, Reason = Reason }, cancellationToken: cancellationToken);
    }
}
