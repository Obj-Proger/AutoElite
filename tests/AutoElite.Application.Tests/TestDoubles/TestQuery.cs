namespace AutoElite.Application.Tests.TestDoubles;

internal sealed record TestQuery(int Value) : IQuery<int>;

internal sealed class TestQueryHandler : IQueryHandler<TestQuery, int>
{
    public Task<Result<int>> Handle(TestQuery request, CancellationToken cancellationToken) =>
        Task.FromResult<Result<int>>(request.Value * 2);
}
