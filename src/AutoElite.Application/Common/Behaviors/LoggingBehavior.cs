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
        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        if (response is Result { IsFailure: true } result)
            logger.LogWarning("{RequestName} failed: [{ErrorCode}] {ErrorMessage}", requestName, result.Error.Code, result.Error.Message);
        else
            logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}
