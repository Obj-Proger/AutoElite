using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A branch (office) of the driving school. Leads, contracts, groups, training grounds and staff can be tied to it.
/// </summary>
/// <remarks>
/// Lessons and other instants are stored in UTC; the branch's time zone is used to show them in local time
/// and to schedule reminders. Uniqueness of the branch name is checked by the application layer.
/// </remarks>
public sealed class Branch : AuditableEntity
{
    private const int MaxNameLength = 200;

    /// <summary>Gets the branch name, unique within the school.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Gets the branch address.</summary>
    public Address Address { get; private set; } = null!;

    /// <summary>Gets the branch's contact phone number, or <see langword="null"/> if not provided.</summary>
    public PhoneNumber? Phone { get; private set; }

    /// <summary>Gets the IANA identifier of the branch's time zone, for example <c>Asia/Yekaterinburg</c>.</summary>
    public string TimeZoneId { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the branch is active.
    /// Deactivated branches are kept for history but cannot be modified.
    /// </summary>
    public bool IsActive { get; private set; }

    private Branch(Guid id, string name, Address address, PhoneNumber? phone, string timeZoneId) : base(id)
    {
        Name = name;
        Address = address;
        Phone = phone;
        TimeZoneId = timeZoneId;
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Branch()
    {
    }

    /// <summary>Creates a new active branch.</summary>
    /// <param name="name">The branch name. Required.</param>
    /// <param name="address">The validated address.</param>
    /// <param name="phone">The validated contact phone number, if any.</param>
    /// <param name="timeZoneId">The IANA time zone identifier, for example <c>Europe/Moscow</c>.</param>
    /// <returns>The created branch, or the first validation failure encountered.</returns>
    public static Result<Branch> Create(string? name, Address address, PhoneNumber? phone, string? timeZoneId)
    {
        var nameResult = ValidateName(name);
        if (nameResult.IsFailure)
            return Result.Failure<Branch>(nameResult.Error);

        var timeZoneResult = ValidateTimeZoneId(timeZoneId);
        if (timeZoneResult.IsFailure)
            return Result.Failure<Branch>(timeZoneResult.Error);

        return new Branch(Guid.CreateVersion7(), nameResult.Value, address, phone, timeZoneResult.Value);
    }

    /// <summary>
    /// Updates the branch details. Passing <see langword="null"/> for <paramref name="phone"/> removes the phone number.
    /// </summary>
    /// <returns>A failure if the branch is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(string? name, Address address, PhoneNumber? phone, string? timeZoneId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Branch.CannotModifyInactive);

        var nameResult = ValidateName(name);
        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Error);

        var timeZoneResult = ValidateTimeZoneId(timeZoneId);
        if (timeZoneResult.IsFailure)
            return Result.Failure(timeZoneResult.Error);

        Name = nameResult.Value;
        Address = address;
        Phone = phone;
        TimeZoneId = timeZoneResult.Value;

        return Result.Success();
    }

    /// <summary>Deactivates the branch without deleting any data.</summary>
    /// <returns>A failure if the branch is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Branch.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Reactivates a previously deactivated branch.</summary>
    /// <returns>A failure if the branch is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.Branch.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    private static Result<string> ValidateName(string? name) =>
        Guard.RequiredText(name, MaxNameLength, DomainErrors.Branch.NameEmpty, DomainErrors.Branch.NameTooLong);

    private static Result<string> ValidateTimeZoneId(string? timeZoneId) =>
        Guard.IanaTimeZone(timeZoneId, DomainErrors.Branch.TimeZoneEmpty, DomainErrors.Branch.TimeZoneUnknown);
}
