using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public CancellationToken LastToken { get; private set; }

    public Exception? ExceptionToThrow { get; init; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        SaveCount++;
        LastToken = cancellationToken;

        return Task.CompletedTask;
    }
}
