namespace AutoElite.Application.Common.Pagination;

/// <summary>
/// A query that returns one page of a list. The page number and size are plain properties, so the API binds them
/// straight from the query string (<c>?page=2&amp;pageSize=50</c>).
/// </summary>
/// <remarks>
/// Each paged query needs a validator derived from <see cref="PagedQueryValidator{TQuery}"/>; an architecture test
/// enforces it. Declare the properties with defaults of 1 and <see cref="PageRequest.DefaultPageSize"/>.
/// </remarks>
public interface IPagedQuery
{
    /// <summary>Gets the number of the requested page, starting from 1.</summary>
    int Page { get; }

    /// <summary>Gets the number of items per page, from 1 to <see cref="PageRequest.MaxPageSize"/>.</summary>
    int PageSize { get; }
}
