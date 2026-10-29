namespace AutoElite.Application.Common.Messaging;

/// <summary>The single entry point of the application layer: sends a command or query through the pipeline.</summary>
public interface IDispatcher
{
    /// <summary>Sends a request through the pipeline behaviors to its handler.</summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The command or query to execute.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The response produced by the pipeline or, if a behavior short-circuits, by that behavior.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
