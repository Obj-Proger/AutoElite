namespace AutoElite.Application.Common.Messaging;

/// <summary>A command that changes state and reports only success or failure.</summary>
public interface ICommand : IRequest<Result>;

/// <summary>A command that changes state and returns a value on success.</summary>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>Handles commands of type <typeparamref name="TCommand"/>.</summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>Handles commands of type <typeparamref name="TCommand"/> that return a value.</summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
