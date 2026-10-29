namespace AutoElite.Application.Common.Messaging;

/// <summary>Invokes the next step of the pipeline: the next behavior or, at the end, the handler.</summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// A cross-cutting step that wraps request handling, such as logging, validation, or authorization.
/// A behavior may return early without calling <c>next</c> to short-circuit the pipeline.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Runs the behavior around the rest of the pipeline.</summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">Invokes the rest of the pipeline.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
