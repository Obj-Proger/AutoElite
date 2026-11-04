using AutoElite.Application.Common.Abstractions;
using AutoElite.Application.Common.Messaging;

namespace AutoElite.Application.Common.Behaviors;

/// <summary>
/// Saves the changes of a command once its handler succeeds. A failed command saves nothing, so a half-applied change
/// never reaches the database; queries pass through untouched. A concurrency conflict becomes a conflict error.
/// </summary>
/// <remarks>Registered last, so it runs only for requests that passed authorization and validation.</remarks>
internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Checked once per request type, because each closed generic type has its own static field.
    private static readonly bool _isCommand = IsCommand(typeof(TRequest));

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_isCommand)
            return await next();

        var response = await next();

        if (response is not Result { IsSuccess: true })
            return response;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return FailureResponseFactory<TResponse>.Create(ApplicationErrors.Concurrency.Conflict);
        }

        return response;
    }

    private static bool IsCommand(Type requestType) =>
        typeof(ICommand).IsAssignableFrom(requestType)
        || requestType.GetInterfaces().Any(implemented =>
            implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(ICommand<>));
}
