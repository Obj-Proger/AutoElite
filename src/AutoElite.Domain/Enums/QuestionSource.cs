namespace AutoElite.Domain.Enums;

/// <summary>Where a question of the bank comes from (FR-7.2).</summary>
public enum QuestionSource
{
    /// <summary>An official traffic-rules exam question, usually imported.</summary>
    Official = 1,

    /// <summary>A question written by the school.</summary>
    School = 2
}
