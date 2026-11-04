using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed record TestPagedQuery(int Page = 1, int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<int>>, IPagedQuery;

internal sealed class TestPagedQueryValidator : PagedQueryValidator<TestPagedQuery>;
