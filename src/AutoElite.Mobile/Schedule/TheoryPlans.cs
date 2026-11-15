using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The theory lessons a theory instructor plans: a new lesson of one of their groups on a topic of its course, a
/// lesson moved to another time and a lesson cancelled with a reason; the students of the group are notified. These
/// need the network, since the API checks the times; the groups and the topics are read from the API or, without a
/// network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class TheoryPlans(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    // The groups an instructor has at once fit in one page of the API.
    private const int GroupPage = 100;

    /// <summary>Reads the active study groups of the instructor, by name.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The groups.</returns>
    public Task<Cached<IReadOnlyList<StudyGroupListItem>>> ReadGroupsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "study-groups/mine",
            async token => (await createApi().Api.V1.StudyGroups.GetAsync(
                request =>
                {
                    request.QueryParameters.IsActive = true;
                    request.QueryParameters.PageSize = GroupPage;
                },
                token))?.Items,
            StudyGroupListItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the topics of a course, in their order.</summary>
    /// <param name="courseId">The course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The course with its topics.</returns>
    public Task<Cached<CourseDetails>> ReadCourseAsync(Guid courseId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"courses/{courseId:N}"),
            token => createApi().Api.V1.Courses[courseId].GetAsync(cancellationToken: token),
            CourseDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Schedules a lesson of a group, given by the instructor.</summary>
    /// <param name="groupId">The group.</param>
    /// <param name="topicId">A topic of the course of the group.</param>
    /// <param name="start">The start, in the future.</param>
    /// <param name="end">The end; a lesson lasts at most 8 hours.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the lesson is scheduled.</returns>
    public Task ScheduleAsync(Guid groupId, Guid topicId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken) =>
        createApi().Api.V1.TheoryLessons.PostAsync(
            new ScheduleTheoryLessonCommand
            {
                GroupId = groupId,
                TopicId = topicId,
                StartUtc = start.ToUniversalTime(),
                EndUtc = end.ToUniversalTime()
            },
            cancellationToken: cancellationToken);

    /// <summary>Moves a lesson that has no attendance yet.</summary>
    /// <param name="lessonId">The lesson.</param>
    /// <param name="start">The new start, in the future.</param>
    /// <param name="end">The new end.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the lesson is moved.</returns>
    public Task RescheduleAsync(Guid lessonId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken) =>
        createApi().Api.V1.TheoryLessons[lessonId].Time.PutAsync(
            new RescheduleTheoryLessonCommand { LessonId = lessonId, StartUtc = start.ToUniversalTime(), EndUtc = end.ToUniversalTime() },
            cancellationToken: cancellationToken);

    /// <summary>Cancels a lesson that has no attendance yet.</summary>
    /// <param name="lessonId">The lesson.</param>
    /// <param name="reason">Why it is cancelled, which the students read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the lesson is cancelled.</returns>
    public Task CancelAsync(Guid lessonId, string reason, CancellationToken cancellationToken) =>
        createApi().Api.V1.TheoryLessons[lessonId].Cancel.PostAsync(
            new CancelTheoryLessonCommand { LessonId = lessonId, Reason = reason }, cancellationToken: cancellationToken);
}
