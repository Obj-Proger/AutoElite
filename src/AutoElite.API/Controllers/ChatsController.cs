using AutoElite.API.Common;
using AutoElite.Application.Chats;
using AutoElite.Application.Chats.AddChatMember;
using AutoElite.Application.Chats.ChangeChatMemberRole;
using AutoElite.Application.Chats.CreateGroupChat;
using AutoElite.Application.Chats.DeleteChatMessage;
using AutoElite.Application.Chats.GetChatMembers;
using AutoElite.Application.Chats.GetChatMessages;
using AutoElite.Application.Chats.GetMyChats;
using AutoElite.Application.Chats.LeaveChat;
using AutoElite.Application.Chats.MarkChatRead;
using AutoElite.Application.Chats.MuteChat;
using AutoElite.Application.Chats.OpenDirectChat;
using AutoElite.Application.Chats.RemoveChatMember;
using AutoElite.Application.Chats.RenameChat;
using AutoElite.Application.Chats.SendChatMessage;
using AutoElite.Application.Chats.SendDirectMessage;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>The personal chat of the signed-in user with another user.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
public sealed record DirectChat(Guid ChatId);

/// <summary>
/// The chats of the signed-in user: personal chats of two users, group chats that staff create and the chats the
/// system keeps for study groups. Every action works on the chats the user is a member of; the administrators of a
/// group chat manage its title and members. Files are attached to a chat as documents and sent with a message.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/chats")]
public sealed class ChatsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the chats of the signed-in user with the latest message and the number of unread ones.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The chats.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ChatListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChats(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyChatsQuery(), cancellationToken));

    /// <summary>
    /// Creates a group chat with the signed-in staff member as its administrator; the Location header leads to its
    /// messages.
    /// </summary>
    /// <param name="command">The title and the other members.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the chat.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreateGroupChat(CreateGroupChatCommand command, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? RespondCreatedIn(result, nameof(GetMessages), new { chatId = result.Value })
            : Failure(result.Error);
    }

    /// <summary>
    /// Gives the personal chat of the signed-in user and another active user, creating it if they have none yet, so that
    /// files can be attached to it before the first message.
    /// </summary>
    /// <param name="command">The other user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The chat.</returns>
    [HttpPost("direct")]
    [ProducesResponseType<DirectChat>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> OpenDirectChat(OpenDirectChatCommand command, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(new DirectChat(result.Value)) : Failure(result.Error);
    }

    /// <summary>
    /// Sends a personal text message to another active user; the first message creates the personal chat of the pair.
    /// To send files, open the chat, attach them to it and send them with a message of the chat.
    /// </summary>
    /// <param name="command">The recipient and the text.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the message.</returns>
    [HttpPost("direct/messages")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> SendDirectMessage(SendDirectMessageCommand command, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, new CreatedResource(result.Value)) : Failure(result.Error);
    }

    /// <summary>
    /// Reads the messages of a chat, the latest first, a batch at a time: the next batch starts before the oldest message
    /// of the previous one, so new messages do not shift it.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="beforeMessageId">The oldest message of the previous batch; the latest messages when not given.</param>
    /// <param name="limit">The number of messages, from 1 to 100.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The messages.</returns>
    [HttpGet("{chatId:guid}/messages")]
    [ProducesResponseType<IReadOnlyList<ChatMessageItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetMessages(
        Guid chatId,
        Guid? beforeMessageId,
        int limit = GetChatMessagesQuery.DefaultLimit,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(new GetChatMessagesQuery(chatId, beforeMessageId, limit), cancellationToken));

    /// <summary>
    /// Sends a message to a chat, with up to 10 documents attached to the chat beforehand; the Location header leads to
    /// the messages of the chat.
    /// </summary>
    /// <param name="chatId">The chat; it takes the place of the one in the body.</param>
    /// <param name="command">The text and the documents.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the message.</returns>
    [HttpPost("{chatId:guid}/messages")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> SendMessage(Guid chatId, SendChatMessageCommand command, CancellationToken cancellationToken) =>
        RespondCreatedIn(
            await dispatcher.Send(command with { ChatId = chatId }, cancellationToken), nameof(GetMessages), new { chatId });

    /// <summary>Deletes a message of the signed-in user for all members; its text and attachments are erased.</summary>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteMessage(Guid messageId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeleteChatMessageCommand(messageId), cancellationToken));

    /// <summary>
    /// Moves the read position of the signed-in user: messages sent up to the time count as read. The position never
    /// moves back, and a time in the future counts as now.
    /// </summary>
    /// <param name="chatId">The chat; it takes the place of the one in the body.</param>
    /// <param name="command">The time of the latest message read, in UTC.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{chatId:guid}/read-position")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> MarkRead(Guid chatId, MarkChatReadCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ChatId = chatId }, cancellationToken));

    /// <summary>Turns the notifications of a chat off or back on for the signed-in user.</summary>
    /// <param name="chatId">The chat; it takes the place of the one in the body.</param>
    /// <param name="command">Whether the notifications are turned off.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{chatId:guid}/mute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Mute(Guid chatId, MuteChatCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ChatId = chatId }, cancellationToken));

    /// <summary>Renames a group chat.</summary>
    /// <param name="chatId">The chat; it takes the place of the one in the body.</param>
    /// <param name="command">The new title.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{chatId:guid}/title")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Rename(Guid chatId, RenameChatCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ChatId = chatId }, cancellationToken));

    /// <summary>Lists the current members of a chat with their roles and read positions.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The members.</returns>
    [HttpGet("{chatId:guid}/members")]
    [ProducesResponseType<IReadOnlyList<ChatMemberItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetMembers(Guid chatId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetChatMembersQuery(chatId), cancellationToken));

    /// <summary>Adds an active user to a group chat.</summary>
    /// <param name="chatId">The chat; it takes the place of the one in the body.</param>
    /// <param name="command">The user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{chatId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AddMember(Guid chatId, AddChatMemberCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ChatId = chatId }, cancellationToken));

    /// <summary>Makes a member of a group chat an administrator or a plain member; the chat keeps an administrator.</summary>
    /// <param name="chatId">The chat; it takes the place of the one in the body.</param>
    /// <param name="userId">The member; it takes the place of the one in the body.</param>
    /// <param name="command">The role.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{chatId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeMemberRole(
        Guid chatId, Guid userId, ChangeChatMemberRoleCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ChatId = chatId, UserId = userId }, cancellationToken));

    /// <summary>Removes a member from a group chat; the member sees no new messages.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userId">The member.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpDelete("{chatId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RemoveMember(Guid chatId, Guid userId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new RemoveChatMemberCommand(chatId, userId), cancellationToken));

    /// <summary>
    /// Leaves a group chat; when the last administrator leaves, the longest-standing member becomes the administrator.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{chatId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Leave(Guid chatId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new LeaveChatCommand(chatId), cancellationToken));
}
