using Microsoft.Extensions.Logging;

namespace AutoElite.Application.Common.Behaviors;

/// <summary>
/// Log messages of the pipeline behaviors and the domain event publisher. The source generator turns each method
/// into a cached delegate, so a message is formatted only when its level is enabled (analyzer rule CA1848).
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    public static partial void HandlingRequest(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName}")]
    public static partial void HandledRequest(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} failed: [{ErrorCode}] {ErrorMessage}")]
    public static partial void RequestFailed(ILogger logger, string requestName, string errorCode, string errorMessage);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {EventName} to {HandlerCount} handlers")]
    public static partial void PublishedDomainEvent(ILogger logger, string eventName, int handlerCount);
}
