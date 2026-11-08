using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Common;

/// <summary>
/// Answers what reached the host as an exception rather than as a result of the application. A request the server
/// could not read, such as one with a body larger than allowed, is the client's error and keeps its status; a request
/// the client abandoned gets no answer; anything else is a failure of the server, logged with the request and answered
/// with a 500 that tells nothing about its cause.
/// </summary>
/// <param name="problemDetails">The writer of problem details.</param>
/// <param name="logger">The logger.</param>
internal sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;

            return true;
        }

        var status = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError;
        if (status >= StatusCodes.Status500InternalServerError)
            LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status }
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The request {Method} {Path} failed with an unhandled exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, PathString path);
}
