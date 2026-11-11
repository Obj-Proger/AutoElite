using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Exams;
using AutoElite.Application.Exams.GetGroupExams;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.StudyGroups.CreateStudyGroup;
using AutoElite.Application.StudyGroups.DeactivateStudyGroup;
using AutoElite.Application.StudyGroups.EnrollInStudyGroup;
using AutoElite.Application.StudyGroups.GetStudyGroup;
using AutoElite.Application.StudyGroups.GetStudyGroups;
using AutoElite.Application.StudyGroups.ReactivateStudyGroup;
using AutoElite.Application.StudyGroups.UpdateStudyGroup;
using AutoElite.Application.StudyGroups.WithdrawFromStudyGroup;
using AutoElite.Application.TheoryLessons;
using AutoElite.Application.TheoryLessons.GetGroupTheoryLessons;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The study groups of the theory course and their students: formed and changed by managers and administrators, read
/// by theory instructors, who see only their own groups. A manager works with the groups of their own branch.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/study-groups")]
public sealed class StudyGroupsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Finds study groups, the latest start first, a page at a time.</summary>
    /// <param name="search">Text in the name of the group.</param>
    /// <param name="courseId">The course.</param>
    /// <param name="theoryInstructorId">The responsible theory instructor.</param>
    /// <param name="branchId">The branch.</param>
    /// <param name="isActive">Whether the groups are active; all when not given.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of groups on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of groups.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<StudyGroupListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudyGroups(
        string? search,
        Guid? courseId,
        Guid? theoryInstructorId,
        Guid? branchId,
        bool? isActive,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(
            new GetStudyGroupsQuery(search, courseId, theoryInstructorId, branchId, isActive, page, pageSize), cancellationToken));

    /// <summary>Reads a study group with all its enrollments, current and past.</summary>
    /// <param name="groupId">The group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The group.</returns>
    [HttpGet("{groupId:guid}")]
    [ProducesResponseType<StudyGroupDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetStudyGroup(Guid groupId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetStudyGroupQuery(groupId), cancellationToken));

    /// <summary>
    /// Forms a study group for an active course with a responsible theory instructor; a group of a manager goes to the
    /// manager's branch unless another branch is given.
    /// </summary>
    /// <param name="command">The course, the instructor, the dates and the capacity.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the group.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateStudyGroup(CreateStudyGroupCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetStudyGroup), "groupId");

    /// <summary>Changes an active group; the capacity cannot drop below the number of its current students.</summary>
    /// <param name="groupId">The group; it takes the place of the one in the body.</param>
    /// <param name="command">The name, the instructor, the dates and the capacity.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateStudyGroup(
        Guid groupId, UpdateStudyGroupCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { GroupId = groupId }, cancellationToken));

    /// <summary>Closes a group whose training has finished or is cancelled; its students can still be withdrawn.</summary>
    /// <param name="groupId">The group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{groupId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid groupId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateStudyGroupCommand(groupId), cancellationToken));

    /// <summary>Opens a closed group again.</summary>
    /// <param name="groupId">The group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{groupId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid groupId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateStudyGroupCommand(groupId), cancellationToken));

    /// <summary>
    /// Enrolls the student of an active contract of the same course in the group, if it has a free place; the Location
    /// header leads to the group.
    /// </summary>
    /// <param name="groupId">The group; it takes the place of the one in the body.</param>
    /// <param name="command">The contract of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the enrollment.</returns>
    [HttpPost("{groupId:guid}/members")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Enroll(Guid groupId, EnrollInStudyGroupCommand command, CancellationToken cancellationToken) =>
        RespondCreatedIn(
            await dispatcher.Send(command with { GroupId = groupId }, cancellationToken), nameof(GetStudyGroup), new { groupId });

    /// <summary>
    /// Withdraws the student of a contract from the group, for example to move them to another group; the enrollment
    /// stays in the history of the group.
    /// </summary>
    /// <param name="groupId">The group.</param>
    /// <param name="contractId">The contract of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("{groupId:guid}/members/{contractId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Withdraw(Guid groupId, Guid contractId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new WithdrawFromStudyGroupCommand(groupId, contractId), cancellationToken));

    /// <summary>Lists all theory lessons of a group, cancelled ones included, the earliest first.</summary>
    /// <param name="groupId">The group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The lessons.</returns>
    [HttpGet("{groupId:guid}/theory-lessons")]
    [ProducesResponseType<IReadOnlyList<TheoryLessonItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTheoryLessons(Guid groupId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetGroupTheoryLessonsQuery(groupId), cancellationToken));

    /// <summary>Lists all exams of a group, cancelled ones included, the earliest first.</summary>
    /// <param name="groupId">The group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The exams.</returns>
    [HttpGet("{groupId:guid}/exams")]
    [ProducesResponseType<IReadOnlyList<ExamItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetExams(Guid groupId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetGroupExamsQuery(groupId), cancellationToken));
}
