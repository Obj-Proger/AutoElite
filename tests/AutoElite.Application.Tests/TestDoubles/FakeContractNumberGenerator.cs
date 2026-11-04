using AutoElite.Application.Contracts;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeContractNumberGenerator : IContractNumberGenerator
{
    private int _last;

    public Task<string> NextAsync(DateOnly startDate, CancellationToken cancellationToken = default) =>
        Task.FromResult($"{startDate.Year}-{++_last:D6}");
}
