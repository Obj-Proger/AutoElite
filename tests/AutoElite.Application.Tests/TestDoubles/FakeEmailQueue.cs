using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeEmailQueue : IEmailQueue
{
    private readonly List<EmailMessage> _messages = [];

    public IReadOnlyList<EmailMessage> Messages => _messages;

    public Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add(message);

        return Task.CompletedTask;
    }
}
