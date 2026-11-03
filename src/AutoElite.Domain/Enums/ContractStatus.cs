namespace AutoElite.Domain.Enums;

/// <summary>The status of a training contract. <see cref="Completed"/> and <see cref="Terminated"/> are final.</summary>
public enum ContractStatus
{
    /// <summary>Training is in progress.</summary>
    Active = 1,

    /// <summary>Training finished successfully and nothing is owed.</summary>
    Completed = 2,

    /// <summary>The contract was ended early; the reason is recorded.</summary>
    Terminated = 3
}
