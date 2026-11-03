namespace AutoElite.Domain.Enums;

/// <summary>The stage of a lead in the sales funnel.</summary>
/// <remarks>
/// Allowed transitions: <see cref="New"/> to <see cref="InProgress"/>, <see cref="Converted"/> or <see cref="Lost"/>;
/// <see cref="InProgress"/> to <see cref="Converted"/> or <see cref="Lost"/>; <see cref="Lost"/> back to
/// <see cref="InProgress"/>. <see cref="Converted"/> is final.
/// </remarks>
public enum LeadStatus
{
    /// <summary>Just received; nobody has worked on it yet.</summary>
    New = 1,

    /// <summary>A manager is working with the prospective student.</summary>
    InProgress = 2,

    /// <summary>A contract was signed; final.</summary>
    Converted = 3,

    /// <summary>The prospective student declined or could not be reached; can be reopened.</summary>
    Lost = 4
}
