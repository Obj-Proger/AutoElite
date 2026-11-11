using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StaffTasks;
using AutoElite.Application.StaffTasks.CompleteStaffTask;
using AutoElite.Application.StaffTasks.CreateStaffTask;
using AutoElite.Application.StaffTasks.GetMyStaffTasks;
using AutoElite.Application.StaffTasks.GetStaffTask;
using AutoElite.Application.StaffTasks.GetStaffTasks;
using AutoElite.Application.StaffTasks.ReassignStaffTask;
using AutoElite.Application.StaffTasks.ReopenStaffTask;
using AutoElite.Application.StaffTasks.StartStaffTask;
using AutoElite.Application.StaffTasks.UpdateStaffTask;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The tasks of the staff: given, changed and reassigned by managers and administrators, worked on by their assignees.
/// A manager manages the tasks of the staff of their own branch.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/staff-tasks")]
public sealed class StaffTasksController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Finds the tasks the signed-in manager or administrator manages, the nearest deadline first.</summary>
    /// <param name="assigneeId">The staff member responsible.</param>
    /// <param name="status">The status of the tasks.</param>
    /// <param name="overdueOnly">Only open tasks past their deadline.</param>
    /// <param name="subjectKind">The kind of the record the tasks are about, for example a lead.</param>
    /// <param name="subjectId">The record; given together with the kind.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of tasks on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of tasks.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<StaffTaskListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        Guid? assigneeId,
        StaffTaskStatus? status,
        bool overdueOnly = false,
        EntityKind? subjectKind = null,
        Guid? subjectId = null,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(
            new GetStaffTasksQuery(assigneeId, status, overdueOnly, subjectKind, subjectId, page, pageSize), cancellationToken));

    /// <summary>Reads the tasks of the signed-in staff member, the nearest deadline first.</summary>
    /// <param name="status">The status of the tasks.</param>
    /// <param name="overdueOnly">Only open tasks past their deadline.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of tasks on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of tasks.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/me/staff-tasks")]
    [ProducesResponseType<PagedList<StaffTaskListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTasks(
        StaffTaskStatus? status,
        bool overdueOnly = false,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(new GetMyStaffTasksQuery(status, overdueOnly, page, pageSize), cancellationToken));

    /// <summary>Reads a task: for its assignee, or for a manager or administrator who manages it.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The task.</returns>
    [HttpGet("{taskId:guid}")]
    [ProducesResponseType<StaffTaskDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTask(Guid taskId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetStaffTaskQuery(taskId), cancellationToken));

    /// <summary>
    /// Gives a staff member a task with a deadline in the future, optionally about a record and repeating; the assignee
    /// is notified.
    /// </summary>
    /// <param name="command">The title, the assignee, the deadline, the record and the recurrence.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the task.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreateTask(CreateStaffTaskCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetTask), "taskId");

    /// <summary>
    /// Changes the title, the description, the deadline and the record of an open task; a new deadline allows a new
    /// overdue reminder.
    /// </summary>
    /// <param name="taskId">The task; it takes the place of the one in the body.</param>
    /// <param name="command">The title, the description, the deadline and the record.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateTask(Guid taskId, UpdateStaffTaskCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { TaskId = taskId }, cancellationToken));

    /// <summary>Gives an open task to another staff member, who is notified.</summary>
    /// <param name="taskId">The task; it takes the place of the one in the body.</param>
    /// <param name="command">The new assignee.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{taskId:guid}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reassign(Guid taskId, ReassignStaffTaskCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { TaskId = taskId }, cancellationToken));

    /// <summary>Starts work on a new task.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{taskId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Start(Guid taskId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new StartStaffTaskCommand(taskId), cancellationToken));

    /// <summary>Completes an open task; a repeating task gets its next occurrence.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{taskId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Complete(Guid taskId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new CompleteStaffTaskCommand(taskId), cancellationToken));

    /// <summary>Returns a done task to work; the next occurrence of a repeating task, already created, stays.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{taskId:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reopen(Guid taskId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReopenStaffTaskCommand(taskId), cancellationToken));
}
