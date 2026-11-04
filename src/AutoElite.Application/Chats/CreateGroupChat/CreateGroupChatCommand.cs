namespace AutoElite.Application.Chats.CreateGroupChat;

/// <summary>
/// Creates a group chat with the signed-in staff member as its administrator (FR-9.1). The chats of study groups are
/// created and kept by the system.
/// </summary>
/// <param name="Title">The title of the chat.</param>
/// <param name="MemberIds">The other members, active users of the school.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor, UserRole.DrivingInstructor)]
public sealed record CreateGroupChatCommand(string? Title, IReadOnlyList<Guid>? MemberIds) : ICommand<Guid>;
