using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>How an exam and a result read in the application.</summary>
internal static class ExamTexts
{
    /// <summary>The length of an exam on the schedule, which knows only when it starts.</summary>
    public static readonly TimeSpan ShownLength = TimeSpan.FromHours(1);

    /// <summary>Gives the title of an exam: the test of a theory exam, or that it is a practice one.</summary>
    /// <param name="exam">The exam.</param>
    /// <returns>The title.</returns>
    public static string TitleOf(ExamItem exam) =>
        exam.Kind == ExamKind.Theory
            ? exam.TestTemplateName ?? AppTexts.Get("Exams.Theory")
            : AppTexts.Get("Exams.Practice");

    /// <summary>Gives a result: passed or not, with the score.</summary>
    /// <param name="result">The result.</param>
    /// <returns>The result as the user reads it.</returns>
    public static string ResultOf(ExamResultItem result) =>
        LessonTexts.DetailsOf(
            result.Passed == true ? AppTexts.Get("Test.Passed") : AppTexts.Get("Test.Failed"),
            result.Score is { } score ? AppTexts.Format("Exams.Score", score) : null);
}
