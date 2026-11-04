using AutoElite.Application.Settings;

namespace AutoElite.Application.DrivingSlots.RateDrivingLesson;

/// <summary>Checks that the lesson is the caller's and rates it under the school's low rating threshold.</summary>
internal sealed class RateDrivingLessonCommandHandler(
    IDrivingSlotRepository slotRepository,
    SchoolSettingsProvider settingsProvider,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<RateDrivingLessonCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RateDrivingLessonCommand command, CancellationToken cancellationToken)
    {
        var slot = await slotRepository.GetByIdAsync(command.SlotId, cancellationToken);
        if (slot is null || slot.CurrentBooking?.StudentId != currentUser.GetRequiredUserId())
            return Result.Failure(ApplicationErrors.NotFound<DrivingSlot>());

        var settings = await settingsProvider.GetAsync(cancellationToken);

        return slot.Rate(command.Rating, command.Comment, settings.LowRatingThreshold, timeProvider.GetUtcNow().UtcDateTime);
    }
}
