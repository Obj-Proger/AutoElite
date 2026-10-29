namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FirstBehavior<TRequest, TResponse>(CallJournal journal) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        journal.Add("first:before");
        var response = await next();
        journal.Add("first:after");

        return response;
    }
}

internal sealed class SecondBehavior<TRequest, TResponse>(CallJournal journal) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        journal.Add("second:before");
        var response = await next();
        journal.Add("second:after");

        return response;
    }
}

internal sealed class ShortCircuitBehavior(Error error) : IPipelineBehavior<TestCommand, Result>
{
    public Task<Result> Handle(TestCommand request, RequestHandlerDelegate<Result> next, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure(error));
}
