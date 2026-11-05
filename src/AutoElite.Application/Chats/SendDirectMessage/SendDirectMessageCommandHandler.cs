namespace AutoElite.Application.Chats.SendDirectMessage;

/// <summary>Finds or creates the personal chat of the pair and sends the message.</summary>
internal sealed class SendDirectMessageCommandHandler(
    DirectChats directChats,
    IRepository<ChatMessage> messageRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<SendDirectMessageCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(SendDirectMessageCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var chatResult = await directChats.GetOrCreateAsync(command.RecipientId, utcNow, cancellationToken);
        if (chatResult.IsFailure)
            return Result.Failure<Guid>(chatResult.Error);

        var messageResult = ChatMessage.Send(chatResult.Value, currentUser.GetRequiredUserId(), command.Text, [], utcNow);
        if (messageResult.IsFailure)
            return Result.Failure<Guid>(messageResult.Error);

        messageRepository.Add(messageResult.Value);

        return messageResult.Value.Id;
    }
}
