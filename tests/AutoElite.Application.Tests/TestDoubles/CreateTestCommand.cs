namespace AutoElite.Application.Tests.TestDoubles;

internal sealed record CreateTestCommand(string Name) : ICommand<Guid>;
