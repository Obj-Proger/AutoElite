namespace AutoElite.Application.Common.Messaging;

/// <summary>A query that reads state without changing it and returns a value on success.</summary>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

/// <summary>Handles queries of type <typeparamref name="TQuery"/>.</summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
