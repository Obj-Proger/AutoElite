using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;

namespace AutoElite.Client.Offline.Actions;

/// <summary>A student rates a completed driving lesson, once.</summary>
/// <param name="SlotId">The slot of the lesson.</param>
/// <param name="Rating">The rating, from 1 to 5.</param>
/// <param name="Comment">A comment, if any.</param>
public sealed record RateDrivingLesson(Guid SlotId, int Rating, string? Comment = null) : OfflineAction
{
    /// <inheritdoc />
    public override Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);

        return api.Api.V1.DrivingSlots[SlotId].Rating.PostAsync(
            new RateDrivingLessonCommand { SlotId = SlotId, Rating = Rating, Comment = Comment }, cancellationToken: cancellationToken);
    }
}
