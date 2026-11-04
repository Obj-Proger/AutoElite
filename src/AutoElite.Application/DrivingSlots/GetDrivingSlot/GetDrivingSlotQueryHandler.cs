namespace AutoElite.Application.DrivingSlots.GetDrivingSlot;

/// <summary>Reads the card and hides slots the caller may not see as not found.</summary>
internal sealed class GetDrivingSlotQueryHandler(IDrivingSlotQueries slotQueries, ICurrentUser currentUser)
    : IQueryHandler<GetDrivingSlotQuery, DrivingSlotDetails>
{
    /// <inheritdoc />
    public async Task<Result<DrivingSlotDetails>> Handle(GetDrivingSlotQuery query, CancellationToken cancellationToken)
    {
        var details = await slotQueries.GetDetailsAsync(query.SlotId, cancellationToken);

        return details is not null && MayRead(details.Slot)
            ? details
            : Result.Failure<DrivingSlotDetails>(ApplicationErrors.NotFound<DrivingSlot>());
    }

    private bool MayRead(DrivingSlotItem slot)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(slot.BranchId))
            return true;

        return slot.InstructorId == currentUser.UserId;
    }
}
