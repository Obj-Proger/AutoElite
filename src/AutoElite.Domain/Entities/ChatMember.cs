using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Entities;

/// <summary>
/// The membership of one user in a <see cref="Chat"/>. Part of the chat aggregate; a membership that ended is kept as
/// history, and joining again creates a new one.
/// </summary>
public sealed class ChatMember : BaseEntity
{
    /// <summary>Gets the identifier of the user.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the role of the member.</summary>
    public ChatMemberRole Role { get; private set; }

    /// <summary>Gets the UTC time the user joined.</summary>
    public DateTime JoinedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the user left or was removed, or <see langword="null"/> while a member.</summary>
    public DateTime? LeftAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the member turned off notifications for this chat (FR-9.6).</summary>
    public bool IsMuted { get; private set; }

    /// <summary>
    /// Gets the UTC time up to which the member has read the chat: messages sent at or before it are read (FR-9.7);
    /// <see langword="null"/> if nothing has been read.
    /// </summary>
    public DateTime? LastReadAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the user is currently a member.</summary>
    public bool IsCurrent => LeftAtUtc is null;

    private ChatMember(Guid id, Guid userId, ChatMemberRole role, DateTime joinedAtUtc) : base(id)
    {
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private ChatMember()
    {
    }

    internal static ChatMember Join(Guid userId, ChatMemberRole role, DateTime utcNow) =>
        new(Guid.CreateVersion7(), userId, role, utcNow);

    internal void Leave(DateTime utcNow) => LeftAtUtc = utcNow;

    internal void ChangeRole(ChatMemberRole role) => Role = role;

    internal void SetMuted(bool isMuted) => IsMuted = isMuted;

    internal void MarkReadUpTo(DateTime readUpToUtc)
    {
        if (LastReadAtUtc is null || readUpToUtc > LastReadAtUtc)
            LastReadAtUtc = readUpToUtc;
    }
}
