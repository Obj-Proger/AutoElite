namespace AutoElite.Application.Common.Behaviors;

/// <summary>
/// Builds a failed <typeparamref name="TResponse"/> from an <see cref="Error"/>. The response type is always
/// <c>Result</c> or <c>Result&lt;T&gt;</c>, because requests are commands or queries.
/// Shared by every pipeline behavior that can stop the pipeline before the handler runs.
/// </summary>
/// <typeparam name="TResponse">The response type: <see cref="Result"/> or <see cref="Result{TValue}"/>.</typeparam>
internal static class FailureResponseFactory<TResponse>
{
    // The reflection runs once per closed response type, because each closed generic type has its own static field.
    private static readonly Func<Error, TResponse> _factory = CreateFactory();

    /// <summary>Creates a failed response carrying the specified error.</summary>
    public static TResponse Create(Error error) => _factory(error);

    private static Func<Error, TResponse> CreateFactory()
    {
        if (typeof(TResponse) == typeof(Result))
            return error => (TResponse)(object)Result.Failure(error);

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethods()
            .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition)
            .MakeGenericMethod(valueType);

        return (Func<Error, TResponse>)Delegate.CreateDelegate(typeof(Func<Error, TResponse>), failureMethod);
    }
}
