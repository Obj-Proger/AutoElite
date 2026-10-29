using AutoElite.Application.Common.Messaging;
using FluentValidation;

namespace AutoElite.Application.Common.Behaviors;

/// <summary>
/// Runs every registered validator for the request. If any rule fails, short-circuits with a validation
/// error that lists all failed fields, so that the handler never sees an invalid request.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var fieldErrors = new List<FieldError>();

        foreach (var validator in validators)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            fieldErrors.AddRange(validationResult.Errors.Select(failure =>
                new FieldError(failure.PropertyName, failure.ErrorCode ?? ApplicationErrors.InvalidFieldCode, failure.ErrorMessage)));
        }

        return fieldErrors.Count == 0
            ? await next()
            : FailureResponseFactory<TResponse>.Create(ApplicationErrors.ValidationFailed(fieldErrors));
    }
}
