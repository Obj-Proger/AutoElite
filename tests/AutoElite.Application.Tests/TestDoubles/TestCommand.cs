namespace AutoElite.Application.Tests.TestDoubles;

internal sealed record TestCommand(string Name, int Number = 1) : ICommand;

internal sealed record UnhandledCommand : ICommand;

internal sealed class TestCommandHandler(CallJournal journal) : ICommandHandler<TestCommand>
{
    public Task<Result> Handle(TestCommand request, CancellationToken cancellationToken)
    {
        journal.Add("handler");
        journal.LastToken = cancellationToken;

        return Task.FromResult(Result.Success());
    }
}
