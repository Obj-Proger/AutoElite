using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Entities;

/// <summary>One entry of a <see cref="Lead"/>'s status history. Part of the lead aggregate; never changed.</summary>
public sealed class LeadStatusChange : BaseEntity
{
    /// <summary>Gets the status before the change, or <see langword="null"/> for the entry recorded when the lead was created.</summary>
    public LeadStatus? FromStatus { get; private set; }

    /// <summary>Gets the status after the change.</summary>
    public LeadStatus ToStatus { get; private set; }

    /// <summary>Gets the UTC time of the change.</summary>
    public DateTime ChangedAtUtc { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who made the change, or <see langword="null"/> if it was made by the
    /// system or by a visitor submitting the public form.
    /// </summary>
    public Guid? ChangedByUserId { get; private set; }

    /// <summary>Gets the reason for the change, if one was given (required when the lead is lost).</summary>
    public string? Reason { get; private set; }

    private LeadStatusChange(Guid id, LeadStatus? fromStatus, LeadStatus toStatus, DateTime changedAtUtc, Guid? changedByUserId, string? reason)
        : base(id)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAtUtc = changedAtUtc;
        ChangedByUserId = changedByUserId;
        Reason = reason;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private LeadStatusChange()
    {
    }

    /// <summary>Records a change. The lead validates the transition and the arguments before calling this.</summary>
    internal static LeadStatusChange Record(LeadStatus? fromStatus, LeadStatus toStatus, DateTime utcNow, Guid? changedByUserId, string? reason) =>
        new(Guid.CreateVersion7(), fromStatus, toStatus, utcNow, changedByUserId, reason);
}
