namespace AutoElite.Application.Chats;

/// <summary>Loads and adds chats for commands; a loaded chat includes its members.</summary>
public interface IChatRepository : IRepository<Chat>
{
    /// <summary>Loads the personal chat of a pair of users.</summary>
    /// <param name="directKey">The key of the pair, from <see cref="Chat.BuildDirectKey"/>.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The chat, or <see langword="null"/> if the pair has none yet.</returns>
    Task<Chat?> GetDirectAsync(string directKey, CancellationToken cancellationToken = default);
}
