using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>The texts of a test attempt that more than one page shows.</summary>
internal static class TestTexts
{
    /// <summary>Tells how an attempt ended, or that it is in progress.</summary>
    /// <param name="status">The state of the attempt.</param>
    /// <param name="reason">Why it failed, if it did.</param>
    /// <returns>The outcome, in the language of the device.</returns>
    public static string OutcomeOf(TestSessionStatus? status, TestFailureReason? reason) =>
        (status, reason) switch
        {
            (TestSessionStatus.Passed, _) => AppTexts.Get("Test.Passed"),
            (TestSessionStatus.Failed, TestFailureReason.TooManyMistakes) => AppTexts.Get("Test.TooManyMistakes"),
            (TestSessionStatus.Failed, TestFailureReason.TooManyMistakesInBlock) => AppTexts.Get("Test.TooManyMistakesInBlock"),
            (TestSessionStatus.Failed, TestFailureReason.MistakeInAddOnQuestion) => AppTexts.Get("Test.MistakeInAddOn"),
            (TestSessionStatus.Failed, TestFailureReason.TimeExpired) => AppTexts.Get("Test.TimeExpired"),
            (TestSessionStatus.Failed, _) => AppTexts.Get("Test.Failed"),
            _ => AppTexts.Get("Test.InProgress")
        };
}
