using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Application.Common.Messaging;

/// <summary>
/// Non-generic view of <see cref="RequestHandlerWrapper{TRequest, TResponse}"/>, so that the dispatcher
/// can call a wrapper whose request type is only known at runtime.
/// </summary>
internal abstract class RequestHandlerWrapperBase
{
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves the handler and the pipeline behaviors for one request type and chains them together.
/// </summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapperBase
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        Task<TResponse> InvokeHandler() =>
            serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
                .Handle(typedRequest, cancellationToken);

        // Wrapping starts from the innermost step, so the behaviors are reversed:
        // the first registered behavior ends up outermost and runs first.
        var pipeline = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate(
                (RequestHandlerDelegate<TResponse>)InvokeHandler,
                (next, behavior) => () => behavior.Handle(typedRequest, next, cancellationToken));

        return await pipeline();
    }
}
