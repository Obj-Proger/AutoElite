using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Users.GetUsers;

/// <summary>Reads the page within the caller's scope; a branch outside the scope gives an empty page.</summary>
internal sealed class GetUsersQueryHandler(IUserQueries userQueries, ICurrentUser currentUser)
    : IQueryHandler<GetUsersQuery, PagedList<UserListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<UserListItem>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var page = PageRequest.From(query);
        var scope = DataScope.For(currentUser);

        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return PagedList<UserListItem>.Empty(page);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var filter = new UserListFilter(search, query.Role, query.BranchId, query.IsActive);

        return await userQueries.GetListAsync(filter, scope, page, cancellationToken);
    }
}
