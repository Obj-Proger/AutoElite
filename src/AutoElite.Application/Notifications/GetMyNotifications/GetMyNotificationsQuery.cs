using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Notifications.GetMyNotifications;

/// <summary>Reads a page of the notifications of the signed-in user, the latest first.</summary>
/// <param name="IsRead">Only read or only unread notifications.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of notifications per page.</param>
public sealed record GetMyNotificationsQuery(bool? IsRead = null, int Page = 1, int PageSize = PageRequest.DefaultPageSize)
    : IQuery<PagedList<NotificationItem>>, IPagedQuery;
