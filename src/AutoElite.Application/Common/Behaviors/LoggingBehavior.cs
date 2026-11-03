using AutoElite.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace AutoElite.Application.Common.Behaviors;

/// <summary>Logs the start and the outcome of every request.</summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        Log.HandlingRequest(logger, requestName);

        var response = await next();

        if (response is Result { IsFailure: true } result)
            Log.RequestFailed(logger, requestName, result.Error.Code, result.Error.Message);
        else
            Log.HandledRequest(logger, requestName);

        return response;
    }
}
