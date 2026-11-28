using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakePushSender : IPushSender
{
    public List<(IReadOnlyList<string> Tokens, PushMessage Message)> Sent { get; } = [];

    public HashSet<string> UnknownTokens { get; } = [];

    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, PushMessage message, CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
            throw Failure;

        Sent.Add((tokens, message));

        return Task.FromResult<IReadOnlyList<string>>([.. tokens.Where(UnknownTokens.Contains)]);
    }
}
