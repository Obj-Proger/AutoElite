using System.Collections.Concurrent;

namespace AutoElite.Application.Common.Messaging;

/// <summary>Default <see cref="IDispatcher"/>: finds the handler and behaviors for a request through the service provider.</summary>
internal sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapperBase> _wrappers = new();

    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = _wrappers.GetOrAdd(request.GetType(), requestType =>
        {
            var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, typeof(TResponse));

            return (RequestHandlerWrapperBase)Activator.CreateInstance(wrapperType)!;
        });

        var response = await wrapper.Handle(request, serviceProvider, cancellationToken);

        return (TResponse)response!;
    }
}
