using AutoElite.API.Common;
using AutoElite.Application.Courses;
using AutoElite.Application.Courses.AddCourseTopic;
using AutoElite.Application.Courses.CreateCourse;
using AutoElite.Application.Courses.DeactivateCourse;
using AutoElite.Application.Courses.GetCourse;
using AutoElite.Application.Courses.GetCourses;
using AutoElite.Application.Courses.MoveCourseTopic;
using AutoElite.Application.Courses.ReactivateCourse;
using AutoElite.Application.Courses.RemoveCourseTopic;
using AutoElite.Application.Courses.UpdateCourse;
using AutoElite.Application.Courses.UpdateCourseTopic;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The courses of the school and the ordered topics of their theory: read by every signed-in user, managed by
/// administrators.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/courses")]
public sealed class CoursesController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the courses.</summary>
    /// <param name="category">The licence category of the courses.</param>
    /// <param name="isActive">Whether the courses are active; all when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The courses.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses(LicenseCategory? category, bool? isActive, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetCoursesQuery(category, isActive), cancellationToken));

    /// <summary>Reads a course with its topics in order.</summary>
    /// <param name="courseId">The course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The course.</returns>
    [HttpGet("{courseId:guid}")]
    [ProducesResponseType<CourseDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCourse(Guid courseId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetCourseQuery(courseId), cancellationToken));

    /// <summary>Creates a course.</summary>
    /// <param name="command">The course; a price without a currency is in the default currency of the school.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the course.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateCourse(CreateCourseCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetCourse), "courseId");

    /// <summary>Changes a course; contracts already drawn up keep their price.</summary>
    /// <param name="courseId">The course; it takes the place of the one in the body.</param>
    /// <param name="command">The course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{courseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateCourse(Guid courseId, UpdateCourseCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { CourseId = courseId }, cancellationToken));

    /// <summary>Withdraws a course from sale; contracts on it go on.</summary>
    /// <param name="courseId">The course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{courseId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid courseId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateCourseCommand(courseId), cancellationToken));

    /// <summary>Puts a withdrawn course on sale again.</summary>
    /// <param name="courseId">The course.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{courseId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid courseId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateCourseCommand(courseId), cancellationToken));

    /// <summary>Adds a topic at the end of the course; the Location header leads to the course.</summary>
    /// <param name="courseId">The course; it takes the place of the one in the body.</param>
    /// <param name="command">The title and the description of the topic.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the topic.</returns>
    [HttpPost("{courseId:guid}/topics")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> AddTopic(Guid courseId, AddCourseTopicCommand command, CancellationToken cancellationToken) =>
        RespondCreatedIn(
            await dispatcher.Send(command with { CourseId = courseId }, cancellationToken), nameof(GetCourse), new { courseId });

    /// <summary>Changes a topic.</summary>
    /// <param name="courseId">The course; it takes the place of the one in the body.</param>
    /// <param name="topicId">The topic; it takes the place of the one in the body.</param>
    /// <param name="command">The title and the description of the topic.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{courseId:guid}/topics/{topicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateTopic(
        Guid courseId, Guid topicId, UpdateCourseTopicCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { CourseId = courseId, TopicId = topicId }, cancellationToken));

    /// <summary>Moves a topic to another position; the topics between shift by one.</summary>
    /// <param name="courseId">The course; it takes the place of the one in the body.</param>
    /// <param name="topicId">The topic; it takes the place of the one in the body.</param>
    /// <param name="command">The new position, from 1.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{courseId:guid}/topics/{topicId:guid}/position")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> MoveTopic(
        Guid courseId, Guid topicId, MoveCourseTopicCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { CourseId = courseId, TopicId = topicId }, cancellationToken));

    /// <summary>Removes a topic; the topics after it move up.</summary>
    /// <param name="courseId">The course.</param>
    /// <param name="topicId">The topic.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("{courseId:guid}/topics/{topicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RemoveTopic(Guid courseId, Guid topicId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new RemoveCourseTopicCommand(courseId, topicId), cancellationToken));
}
