using System.Text.Json.Serialization;
using AutoElite.Client.Api;
using AutoElite.Client.Offline.Actions;

namespace AutoElite.Client.Offline;

/// <summary>
/// An action of the user that changes data on the API and may be taken without a network: it waits in the
/// <see cref="OfflineQueue"/> until the API can be reached. Each action is kept as JSON under its kind, so an action
/// keeps its kind once released; a new action is a new kind.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(BookDrivingSlot), "driving-slot.book")]
[JsonDerivedType(typeof(CancelDrivingBooking), "driving-slot.cancel-booking")]
[JsonDerivedType(typeof(MarkDrivingNoShow), "driving-slot.no-show")]
[JsonDerivedType(typeof(CompleteDrivingLesson), "driving-slot.complete")]
[JsonDerivedType(typeof(MarkTheoryAttendance), "theory-lesson.attendance")]
[JsonDerivedType(typeof(SendChatMessage), "chat.message")]
[JsonDerivedType(typeof(RateDrivingLesson), "driving-slot.rating")]
public abstract record OfflineAction
{
    /// <summary>Sends the action to the API.</summary>
    /// <param name="api">The client of the API.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the API has accepted the action.</returns>
    public abstract Task SendAsync(AutoEliteApiClient api, CancellationToken cancellationToken);
}
