namespace AutoElite.Application.Common.Messaging;

/// <summary>Marker for a request that expects a response of type <typeparamref name="TResponse"/>.</summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<TResponse>;

/// <summary>Handles requests of type <typeparamref name="TRequest"/>.</summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the request.</summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
