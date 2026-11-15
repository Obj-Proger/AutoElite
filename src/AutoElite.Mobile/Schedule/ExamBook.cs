using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The school exams on the schedule: those of a student with their own results, and those an instructor sees, a
/// driving instructor the exams of the groups of their branch and a theory instructor those of their groups, each with
/// the current members of the group and their results. A driving instructor records and corrects practice results, a
/// theory instructor corrects the theory results of their groups; these need the network, the rest is read from the
/// API or, without a network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class ExamBook(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads the exams of the signed-in student with their own results.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The exams.</returns>
    public Task<Cached<IReadOnlyList<StudentExamItem>>> ReadMineAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "exams/mine",
            token => createApi().Api.V1.Me.Exams.GetAsync(cancellationToken: token),
            StudentExamItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the exams of a week that the signed-in instructor sees.</summary>
    /// <param name="firstDay">The first day of the week.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The exams.</returns>
    public Task<Cached<IReadOnlyList<ExamItem>>> ReadWeekAsync(DateOnly firstDay, CancellationToken cancellationToken)
    {
        var from = ScheduleWeek.StartOf(firstDay);
        var to = ScheduleWeek.StartOf(firstDay.AddDays(ScheduleWeek.Days));

        return cache.GetListAsync(
            ScheduleWeek.KeyOf("exams", firstDay),
            token => createApi().Api.V1.Exams.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = from;
                    request.QueryParameters.ToUtc = to;
                },
                token),
            ExamItem.CreateFromDiscriminatorValue,
            cancellationToken);
    }

    /// <summary>Reads an exam with the current members of its group and their results.</summary>
    /// <param name="examId">The exam.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The exam.</returns>
    public Task<Cached<ExamDetails>> ReadAsync(Guid examId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"exams/{examId:N}"),
            token => createApi().Api.V1.Exams[examId].GetAsync(cancellationToken: token),
            ExamDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Records the result of a member of the group at a practice exam that has begun.</summary>
    /// <param name="examId">The exam.</param>
    /// <param name="contractId">The contract of the student.</param>
    /// <param name="passed">Whether the student passed.</param>
    /// <param name="score">A score from 0 to 1000, or <see langword="null"/>.</param>
    /// <param name="comment">A comment, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the result is recorded.</returns>
    public Task RecordAsync(
        Guid examId, Guid contractId, bool passed, int? score, string? comment, CancellationToken cancellationToken) =>
        createApi().Api.V1.Exams[examId].Results.PostAsync(
            new RecordPracticeExamResultCommand
            {
                ExamId = examId,
                ContractId = contractId,
                Passed = passed,
                Score = score,
                Comment = comment
            },
            cancellationToken: cancellationToken);

    /// <summary>Corrects a recorded result, for example after an appeal.</summary>
    /// <param name="examId">The exam.</param>
    /// <param name="contractId">The contract of the student.</param>
    /// <param name="passed">Whether the student passed.</param>
    /// <param name="score">A score from 0 to 1000, or <see langword="null"/>.</param>
    /// <param name="comment">The reason of the correction.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the result is corrected.</returns>
    public Task CorrectAsync(
        Guid examId, Guid contractId, bool passed, int? score, string? comment, CancellationToken cancellationToken) =>
        createApi().Api.V1.Exams[examId].Results[contractId].PutAsync(
            new CorrectExamResultCommand
            {
                ExamId = examId,
                ContractId = contractId,
                Passed = passed,
                Score = score,
                Comment = comment
            },
            cancellationToken: cancellationToken);
}
