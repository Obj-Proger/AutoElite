namespace AutoElite.Application.Common.Pagination;

/// <summary>One page of a list together with the information a client needs to show the pager.</summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed class PagedList<T>
{
    /// <summary>Initializes a new page.</summary>
    /// <param name="items">The items of the page, at most <see cref="PageRequest.PageSize"/>.</param>
    /// <param name="request">The page that was requested.</param>
    /// <param name="totalCount">The number of items on all pages; not negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">The total is negative or the page holds more items than its size.</exception>
    public PagedList(IReadOnlyList<T> items, PageRequest request, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(items.Count, request.PageSize, nameof(items));

        Items = items;
        Page = request.Page;
        PageSize = request.PageSize;
        TotalCount = totalCount;
    }

    /// <summary>Gets the items of the page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the number of the page, starting from 1.</summary>
    public int Page { get; }

    /// <summary>Gets the requested number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Gets the number of items on all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Gets the number of pages; 0 for an empty list.</summary>
    public int TotalPages => (int)(((long)TotalCount + PageSize - 1) / PageSize);

    /// <summary>Gets a value indicating whether there is a page before this one.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets a value indicating whether there is a page after this one.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Creates an empty page, for example when the caller's data scope holds no records.</summary>
    /// <param name="request">The page that was requested.</param>
    public static PagedList<T> Empty(PageRequest request) => new([], request, 0);
}
