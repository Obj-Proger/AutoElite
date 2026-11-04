namespace AutoElite.Application.Common.Behaviors;

/// <summary>
/// Rejects a request before validation if the caller is anonymous and the request is not open to anonymous callers,
/// or if the caller lacks every role the request requires. Requests are closed by default.
/// </summary>
internal sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Read once per request type, because each closed generic type has its own static field.
    private static readonly AuthorizationRequirement _requirement = AuthorizationRequirement.For(typeof(TRequest));

    /// <inheritdoc />
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_requirement.AllowsAnonymous)
            return next();

        if (!currentUser.IsAuthenticated())
            return Task.FromResult(FailureResponseFactory<TResponse>.Create(ApplicationErrors.Auth.Unauthenticated));

        if (_requirement.Roles.Count > 0 && !_requirement.Roles.Any(currentUser.IsInRole))
            return Task.FromResult(FailureResponseFactory<TResponse>.Create(ApplicationErrors.Auth.Forbidden));

        return next();
    }
}
