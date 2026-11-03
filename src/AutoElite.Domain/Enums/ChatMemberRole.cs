namespace AutoElite.Domain.Enums;

/// <summary>The role of a member in a group chat.</summary>
public enum ChatMemberRole
{
    /// <summary>Reads and writes messages.</summary>
    Member = 1,

    /// <summary>Also manages members, roles and the title.</summary>
    Administrator = 2
}
