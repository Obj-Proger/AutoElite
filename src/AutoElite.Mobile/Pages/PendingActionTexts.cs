using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>Tells the user which action the school did not accept, and why.</summary>
internal static class PendingActionTexts
{
    /// <summary>Describes an action the API refused.</summary>
    /// <param name="action">The refused action.</param>
    /// <returns>The action and the reason, in the language of the device.</returns>
    public static string DescribeRefusal(PendingAction action)
    {
        var what = action.Action switch
        {
            BookDrivingSlot => AppTexts.Get("Action.Book"),
            CancelDrivingBooking => AppTexts.Get("Action.Cancel"),
            MarkDrivingNoShow => AppTexts.Get("Action.NoShow"),
            CompleteDrivingLesson => AppTexts.Get("Action.Complete"),
            MarkTheoryAttendance => AppTexts.Get("Action.Attendance"),
            SendChatMessage => AppTexts.Get("Action.Message"),
            _ => AppTexts.Get("Action.Other")
        };
        var why = action.RejectedWithStatusCode switch
        {
            404 => AppTexts.Get("Refusal.NotFound"),
            409 => AppTexts.Get("Refusal.Conflict"),
            403 => AppTexts.Get("Refusal.Forbidden"),
            _ => AppTexts.Get("Refusal.Other")
        };

        return AppTexts.Format("Home.RefusedItem", what, why);
    }
}
