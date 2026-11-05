using AutoElite.Application.Common.Pagination;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Helpers shared by the read services: paging and text search.</summary>
internal static class QueryExtensions
{
    /// <summary>The character that escapes the wildcards of a LIKE pattern.</summary>
    public const string LikeEscape = "\\";

    /// <summary>Reads the total and one page of an ordered query.</summary>
    /// <param name="ordered">The query, ordered so that pages do not overlap.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <typeparam name="T">The type of the rows.</typeparam>
    /// <returns>The rows of the page and the total.</returns>
    public static async Task<(List<T> Rows, int TotalCount)> ReadPageAsync<T>(
        this IQueryable<T> ordered, PageRequest page, CancellationToken cancellationToken)
    {
        var totalCount = await ordered.CountAsync(cancellationToken);
        if (totalCount <= page.Skip)
            return (new List<T>(), totalCount);

        var rows = await ordered.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return (rows, totalCount);
    }

    /// <summary>
    /// Builds the pattern that finds the text anywhere in a value, with the LIKE wildcards in the text escaped by
    /// <see cref="LikeEscape"/>, so that a search for "50%" finds exactly that.
    /// </summary>
    /// <param name="search">The search text, if any.</param>
    /// <returns>The pattern, or <see langword="null"/> if the text is empty or white space.</returns>
    public static string? ContainsPattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return null;

        var escaped = search.Trim()
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
