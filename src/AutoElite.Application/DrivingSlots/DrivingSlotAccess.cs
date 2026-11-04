namespace AutoElite.Application.DrivingSlots;

/// <summary>Loads a slot for an action of its instructor; for anyone else the slot is reported as not found.</summary>
internal sealed class DrivingSlotAccess(IDrivingSlotRepository slotRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the slot if the caller is its instructor.</summary>
    /// <param name="slotId">The identifier of the slot.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<DrivingSlot>> LoadOwnAsync(Guid slotId, CancellationToken cancellationToken)
    {
        var slot = await slotRepository.GetByIdAsync(slotId, cancellationToken);

        return slot is not null && slot.InstructorId == currentUser.GetRequiredUserId()
            ? slot
            : Result.Failure<DrivingSlot>(ApplicationErrors.NotFound<DrivingSlot>());
    }
}
