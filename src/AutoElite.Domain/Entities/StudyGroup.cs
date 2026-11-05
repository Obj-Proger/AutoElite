using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A study group: students of one course who attend theory lessons together under a responsible theory instructor.
/// Students join through their contracts; the group keeps the full history of enrollments.
/// </summary>
/// <remarks>
/// <para>
/// The capacity is checked against the current members. Two concurrent enrollments could both pass that check, so
/// the persistence layer must treat a change of the enrollments as a change of the group (one concurrency token for
/// the aggregate).
/// </para>
/// <para>
/// A deactivated group (training finished or cancelled) cannot be modified and accepts no new students, but a
/// student can still be withdrawn, so that closing a contract always succeeds.
/// </para>
/// </remarks>
public sealed class StudyGroup : AuditableEntity
{
    /// <summary>The largest number of students a group may hold.</summary>
    public const int MaxCapacity = 100;

    /// <summary>The longest group name.</summary>
    public const int MaxNameLength = 200;

    private readonly List<GroupEnrollment> _enrollments = [];

    /// <summary>Gets the group name.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the identifier of the course the group studies.</summary>
    public Guid CourseId { get; private set; }

    /// <summary>Gets the licence category of the course, copied when the group was formed.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the identifier of the responsible theory instructor.</summary>
    public Guid TheoryInstructorId { get; private set; }

    /// <summary>Gets the identifier of the branch, or <see langword="null"/> if the group is not tied to one.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the first day of training.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Gets the planned last day of training.</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>Gets the maximum number of current members.</summary>
    public int Capacity { get; private set; }

    /// <summary>Gets a value indicating whether the group is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets all enrollments, current and withdrawn, in the order they were made.</summary>
    public IReadOnlyList<GroupEnrollment> Enrollments => _enrollments.AsReadOnly();

    /// <summary>Gets the number of current members.</summary>
    public int CurrentMemberCount => _enrollments.Count(enrollment => enrollment.IsCurrent);

    private StudyGroup(Guid id, Course course, Guid? branchId, GroupDetails details) : base(id)
    {
        CourseId = course.Id;
        Category = course.Category;
        BranchId = branchId;
        Apply(details);
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private StudyGroup()
    {
    }

    /// <summary>Forms a new active group without members.</summary>
    /// <param name="name">The group name. Required.</param>
    /// <param name="course">The course; it must be active. Its identifier and category are copied.</param>
    /// <param name="theoryInstructorId">The responsible theory instructor; checking the role is up to the caller.</param>
    /// <param name="branchId">The branch, if any.</param>
    /// <param name="startDate">The first day of training.</param>
    /// <param name="endDate">The planned last day of training; must be later than the start.</param>
    /// <param name="capacity">The maximum number of students, from 1 to <see cref="MaxCapacity"/>.</param>
    /// <returns>The created group, or the first validation failure encountered.</returns>
    public static Result<StudyGroup> Create(
        string? name, Course course, Guid theoryInstructorId, Guid? branchId, DateOnly startDate, DateOnly endDate, int capacity)
    {
        if (!course.IsActive)
            return Result.Failure<StudyGroup>(DomainErrors.StudyGroup.CourseInactive);

        var detailsResult = ValidateDetails(name, theoryInstructorId, startDate, endDate, capacity, currentMemberCount: 0);
        if (detailsResult.IsFailure)
            return Result.Failure<StudyGroup>(detailsResult.Error);

        if (branchId is { } branch && branch == Guid.Empty)
            return Result.Failure<StudyGroup>(DomainErrors.StudyGroup.BranchIdEmpty);

        return new StudyGroup(Guid.CreateVersion7(), course, branchId, detailsResult.Value);
    }

    /// <summary>Changes the group details. The capacity cannot drop below the current number of members.</summary>
    /// <returns>A failure if the group is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(string? name, Guid theoryInstructorId, DateOnly startDate, DateOnly endDate, int capacity)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.StudyGroup.CannotModifyInactive);

        var detailsResult = ValidateDetails(name, theoryInstructorId, startDate, endDate, capacity, CurrentMemberCount);
        if (detailsResult.IsFailure)
            return Result.Failure(detailsResult.Error);

        Apply(detailsResult.Value);

        return Result.Success();
    }

    /// <summary>Enrolls the student of a contract and raises <see cref="StudentEnrolledDomainEvent"/>.</summary>
    /// <param name="contract">The contract; it must be active and for the group's course.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>
    /// The enrollment, or a failure if the group is inactive or full, the contract is not active or is for another
    /// course, or the contract is already a current member.
    /// </returns>
    public Result<GroupEnrollment> Enroll(Contract contract, DateTime utcNow)
    {
        if (!IsActive)
            return Result.Failure<GroupEnrollment>(DomainErrors.StudyGroup.CannotModifyInactive);

        if (contract.Status != ContractStatus.Active)
            return Result.Failure<GroupEnrollment>(DomainErrors.StudyGroup.ContractNotActive);

        if (contract.CourseId != CourseId)
            return Result.Failure<GroupEnrollment>(DomainErrors.StudyGroup.CourseMismatch);

        if (FindCurrentEnrollment(contract.Id) is not null)
            return Result.Failure<GroupEnrollment>(DomainErrors.StudyGroup.AlreadyEnrolled);

        if (CurrentMemberCount >= Capacity)
            return Result.Failure<GroupEnrollment>(DomainErrors.StudyGroup.GroupFull);

        var enrollment = GroupEnrollment.Create(contract.Id, contract.StudentId, utcNow);
        _enrollments.Add(enrollment);
        RaiseDomainEvent(new StudentEnrolledDomainEvent(Id, contract.Id, contract.StudentId, utcNow));

        return enrollment;
    }

    /// <summary>
    /// Ends the current enrollment of a contract and raises <see cref="StudentWithdrawnDomainEvent"/>.
    /// Allowed even when the group is inactive.
    /// </summary>
    /// <returns>A failure if the contract is not a current member.</returns>
    public Result Withdraw(Guid contractId, DateTime utcNow)
    {
        var enrollment = FindCurrentEnrollment(contractId);
        if (enrollment is null)
            return Result.Failure(DomainErrors.StudyGroup.NotEnrolled);

        enrollment.Withdraw(utcNow);
        RaiseDomainEvent(new StudentWithdrawnDomainEvent(Id, contractId, enrollment.StudentId, utcNow));

        return Result.Success();
    }

    /// <summary>Deactivates the group when training has finished or the group is cancelled.</summary>
    /// <returns>A failure if the group is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.StudyGroup.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Reactivates a previously deactivated group.</summary>
    /// <returns>A failure if the group is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.StudyGroup.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    private GroupEnrollment? FindCurrentEnrollment(Guid contractId) =>
        _enrollments.Find(enrollment => enrollment.IsCurrent && enrollment.ContractId == contractId);

    private void Apply(GroupDetails details)
    {
        Name = details.Name;
        TheoryInstructorId = details.TheoryInstructorId;
        StartDate = details.StartDate;
        EndDate = details.EndDate;
        Capacity = details.Capacity;
    }

    private static Result<GroupDetails> ValidateDetails(
        string? name, Guid theoryInstructorId, DateOnly startDate, DateOnly endDate, int capacity, int currentMemberCount)
    {
        var nameResult = Guard.RequiredText(name, MaxNameLength, DomainErrors.StudyGroup.NameEmpty, DomainErrors.StudyGroup.NameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure<GroupDetails>(nameResult.Error);

        var instructorResult = Guard.NotEmpty(theoryInstructorId, DomainErrors.StudyGroup.InstructorIdEmpty);
        if (instructorResult.IsFailure)
            return Result.Failure<GroupDetails>(instructorResult.Error);

        if (endDate <= startDate)
            return Result.Failure<GroupDetails>(DomainErrors.StudyGroup.EndNotAfterStart);

        if (capacity is < 1 or > MaxCapacity)
            return Result.Failure<GroupDetails>(DomainErrors.StudyGroup.CapacityOutOfRange);

        if (capacity < currentMemberCount)
            return Result.Failure<GroupDetails>(DomainErrors.StudyGroup.CapacityBelowMemberCount);

        return new GroupDetails(nameResult.Value, theoryInstructorId, startDate, endDate, capacity);
    }

    /// <summary>Validated group details, passed between validation and assignment.</summary>
    private sealed record GroupDetails(string Name, Guid TheoryInstructorId, DateOnly StartDate, DateOnly EndDate, int Capacity);
}
