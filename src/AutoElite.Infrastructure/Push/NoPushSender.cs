namespace AutoElite.Infrastructure.Push;

/// <summary>
/// The sender of a host without a Firebase service account: no push notification is sent, and the notifications stay
/// in the applications, which read them through the API.
/// </summary>
internal sealed class NoPushSender : IPushSender
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyList<string> tokens, PushMessage message, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
