using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>A student books a practice slot for one of their contracts.</summary>
/// <param name="SlotId">The slot.</param>
/// <param name="ContractId">The contract of the student.</param>
public sealed record BookDrivingSlot(Guid SlotId, Guid ContractId) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Api.V1.DrivingSlots[SlotId].Book.PostAsync(
            new BookDrivingSlotCommand { SlotId = SlotId, ContractId = ContractId }, cancellationToken: cancellationToken);
    }
}
