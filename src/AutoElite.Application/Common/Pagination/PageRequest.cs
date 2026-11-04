namespace AutoElite.Application.Common.Pagination;

/// <summary>A validated page number and size, passed by handlers to read services.</summary>
public sealed record PageRequest
{
    /// <summary>The page size used when the client does not ask for one.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The largest page size a client may ask for.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The largest page number a client may ask for, which keeps <see cref="Skip"/> within range.</summary>
    public const int MaxPage = 100_000;

    /// <summary>The longest search text a paged list accepts.</summary>
    public const int MaxSearchLength = 200;

    /// <summary>Initializes a new page request.</summary>
    /// <param name="page">The page number, from 1 to <see cref="MaxPage"/>.</param>
    /// <param name="pageSize">The number of items per page, from 1 to <see cref="MaxPageSize"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range; validation should have rejected it earlier.</exception>
    public PageRequest(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, MaxPage);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Gets the number of the page, starting from 1.</summary>
    public int Page { get; }

    /// <summary>Gets the number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Gets the number of items before the page.</summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>Creates the page request of a validated paged query.</summary>
    /// <param name="query">The query that passed its <see cref="PagedQueryValidator{TQuery}"/>.</param>
    public static PageRequest From(IPagedQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return new PageRequest(query.Page, query.PageSize);
    }
}
