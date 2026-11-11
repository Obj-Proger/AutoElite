using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Notifications;
using AutoElite.Application.Notifications.GetMyNotifications;
using AutoElite.Application.Notifications.GetUnreadNotificationCount;
using AutoElite.Application.Notifications.MarkAllNotificationsRead;
using AutoElite.Application.Notifications.MarkNotificationRead;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>A number of notifications.</summary>
/// <param name="Count">The number.</param>
public sealed record NotificationCount(int Count);

/// <summary>
/// The notifications of the signed-in user in the application: new leads, bookings, changes of lessons and exams,
/// payments, messages, tasks and reminders.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/notifications")]
public sealed class NotificationsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Reads the notifications of the signed-in user, the latest first, a page at a time.</summary>
    /// <param name="isRead">Whether the notifications are read; all when not given.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of notifications on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of notifications.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<NotificationItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        bool? isRead,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(new GetMyNotificationsQuery(isRead, page, pageSize), cancellationToken));

    /// <summary>Counts the unread notifications of the signed-in user, for the badge of the bell.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The number of unread notifications.</returns>
    [HttpGet("unread-count")]
    [ProducesResponseType<NotificationCount>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetUnreadNotificationCountQuery(), cancellationToken);

        return result.IsSuccess ? Ok(new NotificationCount(result.Value)) : Failure(result.Error);
    }

    /// <summary>Marks a notification of the signed-in user as read; marking it again changes nothing.</summary>
    /// <param name="notificationId">The notification.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new MarkNotificationReadCommand(notificationId), cancellationToken));

    /// <summary>Marks all notifications of the signed-in user as read.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The number of notifications that were unread.</returns>
    [HttpPost("read-all")]
    [ProducesResponseType<NotificationCount>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new MarkAllNotificationsReadCommand(), cancellationToken);

        return result.IsSuccess ? Ok(new NotificationCount(result.Value)) : Failure(result.Error);
    }
}
