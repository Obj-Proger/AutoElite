using AutoElite.Application.Users;

namespace AutoElite.Application.Chats.SendDirectMessage;

/// <summary>Checks the recipient, finds or creates the personal chat of the pair and sends the message.</summary>
internal sealed class SendDirectMessageCommandHandler(
    IChatRepository chatRepository,
    IRepository<ChatMessage> messageRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<SendDirectMessageCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(SendDirectMessageCommand command, CancellationToken cancellationToken)
    {
        var senderId = currentUser.GetRequiredUserId();
        var recipient = await userRepository.GetByIdAsync(command.RecipientId, cancellationToken);
        if (recipient is not { IsActive: true })
            return Result.Failure<Guid>(ApplicationErrors.NotFound<User>());

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var chat = await chatRepository.GetDirectAsync(Chat.BuildDirectKey(senderId, recipient.Id), cancellationToken);
        if (chat is null)
        {
            var chatResult = Chat.CreateDirect(senderId, recipient.Id, utcNow);
            if (chatResult.IsFailure)
                return Result.Failure<Guid>(chatResult.Error);

            chat = chatResult.Value;
            chatRepository.Add(chat);
        }

        var messageResult = ChatMessage.Send(chat, senderId, command.Text, [], utcNow);
        if (messageResult.IsFailure)
            return Result.Failure<Guid>(messageResult.Error);

        messageRepository.Add(messageResult.Value);

        return messageResult.Value.Id;
    }
}
