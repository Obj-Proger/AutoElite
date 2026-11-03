namespace AutoElite.Domain.Enums;

/// <summary>The kind of a chat.</summary>
public enum ChatKind
{
    /// <summary>A personal chat between exactly two users.</summary>
    Direct = 1,

    /// <summary>A group chat with a title, administrators and any number of members.</summary>
    Group = 2
}
