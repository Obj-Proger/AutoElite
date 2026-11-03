namespace AutoElite.Domain.Entities;

/// <summary>
/// The membership of one contract in a <see cref="StudyGroup"/>. Part of the group aggregate. A withdrawn
/// enrollment is kept as history; enrolling again creates a new one.
/// </summary>
public sealed class GroupEnrollment : BaseEntity
{
    /// <summary>Gets the identifier of the enrolled contract.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>Gets the identifier of the student, copied from the contract for quick lookups.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the UTC time of enrollment.</summary>
    public DateTime EnrolledAtUtc { get; private set; }

    /// <summary>Gets the UTC time the student left the group, or <see langword="null"/> while still a member.</summary>
    public DateTime? WithdrawnAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the student is currently a member of the group.</summary>
    public bool IsCurrent => WithdrawnAtUtc is null;

    private GroupEnrollment(Guid id, Guid contractId, Guid studentId, DateTime enrolledAtUtc) : base(id)
    {
        ContractId = contractId;
        StudentId = studentId;
        EnrolledAtUtc = enrolledAtUtc;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private GroupEnrollment()
    {
    }

    /// <summary>Creates an enrollment. The group checks the rules before calling this.</summary>
    internal static GroupEnrollment Create(Guid contractId, Guid studentId, DateTime utcNow) =>
        new(Guid.CreateVersion7(), contractId, studentId, utcNow);

    /// <summary>Marks the enrollment as ended.</summary>
    internal void Withdraw(DateTime utcNow) => WithdrawnAtUtc = utcNow;
}
