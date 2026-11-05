namespace AutoElite.Application.Chats.OpenDirectChat;

/// <summary>Finds or creates the chat and returns its identifier.</summary>
internal sealed class OpenDirectChatCommandHandler(DirectChats directChats, TimeProvider timeProvider)
    : ICommandHandler<OpenDirectChatCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(OpenDirectChatCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await directChats.GetOrCreateAsync(command.UserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return chatResult.IsFailure ? Result.Failure<Guid>(chatResult.Error) : chatResult.Value.Id;
    }
}
