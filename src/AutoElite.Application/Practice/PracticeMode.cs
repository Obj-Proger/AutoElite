namespace AutoElite.Application.Practice;

/// <summary>Which questions of the bank a student practises.</summary>
public enum PracticeMode
{
    /// <summary>The questions of one topic.</summary>
    Topic = 1,

    /// <summary>Every question of the licence category, one after another: a marathon.</summary>
    Marathon = 2,

    /// <summary>The questions the student last answered wrong.</summary>
    Mistakes = 3
}
