using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A prospective student: a request for training received from the public form or recorded by a manager.
/// The aggregate holds the contact details, the funnel status with its full history, and staff notes.
/// </summary>
/// <remarks>
/// <para>
/// The contact name is free text rather than a <see cref="FullName"/>, because visitors often give only a first
/// name; the student's full name is recorded when the contract is drawn up. At least one way to reach the person
/// (phone or email) is required.
/// </para>
/// <para>
/// Contact details, branch and manager can be changed only while the lead is open (<see cref="LeadStatus.New"/>
/// or <see cref="LeadStatus.InProgress"/>). Notes can be added at any stage.
/// </para>
/// </remarks>
public sealed class Lead : AuditableEntity
{
    /// <summary>The longest contact name.</summary>
    public const int MaxContactNameLength = 200;

    /// <summary>The longest comment.</summary>
    public const int MaxCommentLength = 2000;

    /// <summary>The longest reason a lead was lost.</summary>
    public const int MaxLostReasonLength = 500;

    private readonly List<LeadNote> _notes = [];
    private readonly List<LeadStatusChange> _statusHistory = [];

    /// <summary>Gets the name the person gave.</summary>
    public string ContactName { get; private set; } = null!;

    /// <summary>Gets the contact phone number, or <see langword="null"/> if only an email was given.</summary>
    public PhoneNumber? Phone { get; private set; }

    /// <summary>Gets the contact email address, or <see langword="null"/> if only a phone number was given.</summary>
    public Email? Email { get; private set; }

    /// <summary>Gets the channel through which the lead arrived.</summary>
    public LeadSource Source { get; private set; }

    /// <summary>Gets the licence category the person is interested in, or <see langword="null"/> if not yet known.</summary>
    public LicenseCategory? DesiredCategory { get; private set; }

    /// <summary>Gets the free-text comment from the request or the manager, if any.</summary>
    public string? Comment { get; private set; }

    /// <summary>Gets the identifier of the branch handling the lead, or <see langword="null"/> if not yet decided.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the identifier of the responsible manager, or <see langword="null"/> if nobody is assigned.</summary>
    public Guid? ManagerId { get; private set; }

    /// <summary>Gets the current funnel status.</summary>
    public LeadStatus Status { get; private set; }

    /// <summary>Gets the reason given when the lead was last marked lost, or <see langword="null"/> if it is not lost.</summary>
    public string? LostReason { get; private set; }

    /// <summary>Gets the identifier of the contract the lead was converted into, or <see langword="null"/> if not converted.</summary>
    public Guid? ContractId { get; private set; }

    /// <summary>Gets the notes in the order they were written.</summary>
    public IReadOnlyList<LeadNote> Notes => _notes.AsReadOnly();

    /// <summary>Gets every status change in chronological order, starting with the creation of the lead.</summary>
    public IReadOnlyList<LeadStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    /// <summary>Gets a value indicating whether the lead is still being worked on (new or in progress).</summary>
    public bool IsOpen => Status is LeadStatus.New or LeadStatus.InProgress;

    private Lead(Guid id, LeadContact contact, LeadSource source, Guid? branchId) : base(id)
    {
        ApplyContact(contact);
        Source = source;
        BranchId = branchId;
        Status = LeadStatus.New;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Lead()
    {
    }

    /// <summary>Creates a new lead with status <see cref="LeadStatus.New"/> and raises <see cref="LeadCreatedDomainEvent"/>.</summary>
    /// <param name="contactName">The name the person gave. Required.</param>
    /// <param name="phone">The validated phone number, if given.</param>
    /// <param name="email">The validated email address, if given.</param>
    /// <param name="source">The channel through which the lead arrived.</param>
    /// <param name="desiredCategory">The licence category of interest, if known.</param>
    /// <param name="comment">A free-text comment, if any.</param>
    /// <param name="branchId">The branch handling the lead, if known.</param>
    /// <param name="createdByUserId">The staff member recording the lead, or <see langword="null"/> for the public form.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The created lead, or the first validation failure encountered.</returns>
    public static Result<Lead> Create(
        string? contactName,
        PhoneNumber? phone,
        Email? email,
        LeadSource source,
        LicenseCategory? desiredCategory,
        string? comment,
        Guid? branchId,
        Guid? createdByUserId,
        DateTime utcNow)
    {
        var contactResult = ValidateContact(contactName, phone, email, desiredCategory, comment);
        if (contactResult.IsFailure)
            return Result.Failure<Lead>(contactResult.Error);

        var sourceResult = Guard.DefinedEnum(source, DomainErrors.Lead.SourceNotDefined);
        if (sourceResult.IsFailure)
            return Result.Failure<Lead>(sourceResult.Error);

        var branchResult = ValidateOptionalId(branchId, DomainErrors.Lead.BranchIdEmpty);
        if (branchResult.IsFailure)
            return Result.Failure<Lead>(branchResult.Error);

        var actorResult = ValidateOptionalId(createdByUserId, DomainErrors.Lead.UserIdEmpty);
        if (actorResult.IsFailure)
            return Result.Failure<Lead>(actorResult.Error);

        var lead = new Lead(Guid.CreateVersion7(), contactResult.Value, source, branchId);
        lead._statusHistory.Add(LeadStatusChange.Record(null, LeadStatus.New, utcNow, createdByUserId, null));
        lead.RaiseDomainEvent(new LeadCreatedDomainEvent(lead.Id, branchId, source, utcNow));

        return lead;
    }

    /// <summary>
    /// Replaces the contact details. Passing <see langword="null"/> for an optional value clears it,
    /// but at least one of <paramref name="phone"/> and <paramref name="email"/> must remain.
    /// </summary>
    /// <returns>A failure if the lead is not open, or the first validation failure encountered.</returns>
    public Result UpdateContact(string? contactName, PhoneNumber? phone, Email? email, LicenseCategory? desiredCategory, string? comment)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Lead.NotOpen);

        var contactResult = ValidateContact(contactName, phone, email, desiredCategory, comment);
        if (contactResult.IsFailure)
            return Result.Failure(contactResult.Error);

        ApplyContact(contactResult.Value);

        return Result.Success();
    }

    /// <summary>Moves the lead to another branch, or removes the branch when <paramref name="branchId"/> is <see langword="null"/>.</summary>
    /// <returns>A failure if the lead is not open, or <paramref name="branchId"/> is an empty GUID.</returns>
    public Result AssignBranch(Guid? branchId)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Lead.NotOpen);

        var branchResult = ValidateOptionalId(branchId, DomainErrors.Lead.BranchIdEmpty);
        if (branchResult.IsFailure)
            return branchResult;

        BranchId = branchId;

        return Result.Success();
    }

    /// <summary>Makes a manager responsible for the lead and raises <see cref="LeadAssignedDomainEvent"/>.</summary>
    /// <param name="managerId">The manager to assign. Checking that the user is an active manager is up to the caller.</param>
    /// <param name="assignedByUserId">The user making the assignment, or <see langword="null"/> for the system.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the lead is not open, an identifier is empty, or this manager is already assigned.</returns>
    public Result AssignManager(Guid managerId, Guid? assignedByUserId, DateTime utcNow)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Lead.NotOpen);

        var managerResult = Guard.NotEmpty(managerId, DomainErrors.Lead.ManagerIdEmpty);
        if (managerResult.IsFailure)
            return Result.Failure(managerResult.Error);

        var actorResult = ValidateOptionalId(assignedByUserId, DomainErrors.Lead.UserIdEmpty);
        if (actorResult.IsFailure)
            return actorResult;

        if (ManagerId == managerId)
            return Result.Failure(DomainErrors.Lead.ManagerAlreadyAssigned);

        ManagerId = managerId;
        RaiseDomainEvent(new LeadAssignedDomainEvent(Id, managerId, assignedByUserId, utcNow));

        return Result.Success();
    }

    /// <summary>Moves a new lead to <see cref="LeadStatus.InProgress"/>.</summary>
    /// <returns>A failure if the lead is not new, or <paramref name="changedByUserId"/> is an empty GUID.</returns>
    public Result StartWork(Guid? changedByUserId, DateTime utcNow)
    {
        if (Status != LeadStatus.New)
            return Result.Failure(DomainErrors.Lead.NotNew);

        return ChangeStatus(LeadStatus.InProgress, changedByUserId, null, utcNow);
    }

    /// <summary>Returns a lost lead to <see cref="LeadStatus.InProgress"/> and clears the lost reason.</summary>
    /// <returns>A failure if the lead is not lost, or <paramref name="changedByUserId"/> is an empty GUID.</returns>
    public Result Reopen(Guid? changedByUserId, DateTime utcNow)
    {
        if (Status != LeadStatus.Lost)
            return Result.Failure(DomainErrors.Lead.NotLost);

        var result = ChangeStatus(LeadStatus.InProgress, changedByUserId, null, utcNow);
        if (result.IsSuccess)
            LostReason = null;

        return result;
    }

    /// <summary>Marks an open lead as <see cref="LeadStatus.Lost"/>.</summary>
    /// <param name="reason">Why the lead was lost. Required, so that losses can be analysed.</param>
    /// <param name="changedByUserId">The user making the change, or <see langword="null"/> for the system.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the lead is not open, the reason is empty or too long, or the user ID is empty.</returns>
    public Result MarkLost(string? reason, Guid? changedByUserId, DateTime utcNow)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Lead.NotOpen);

        var reasonResult = Guard.RequiredText(reason, MaxLostReasonLength, DomainErrors.Lead.LostReasonEmpty, DomainErrors.Lead.LostReasonTooLong);
        if (reasonResult.IsFailure)
            return Result.Failure(reasonResult.Error);

        var result = ChangeStatus(LeadStatus.Lost, changedByUserId, reasonResult.Value, utcNow);
        if (result.IsSuccess)
            LostReason = reasonResult.Value;

        return result;
    }

    /// <summary>
    /// Marks an open lead as <see cref="LeadStatus.Converted"/> into the specified contract and raises
    /// <see cref="LeadConvertedDomainEvent"/>. The caller creates the contract in the same unit of work.
    /// </summary>
    /// <returns>A failure if the lead is not open, or an identifier is empty.</returns>
    public Result MarkConverted(Guid contractId, Guid? changedByUserId, DateTime utcNow)
    {
        if (!IsOpen)
            return Result.Failure(DomainErrors.Lead.NotOpen);

        var contractResult = Guard.NotEmpty(contractId, DomainErrors.Lead.ContractIdEmpty);
        if (contractResult.IsFailure)
            return Result.Failure(contractResult.Error);

        var result = ChangeStatus(LeadStatus.Converted, changedByUserId, null, utcNow);
        if (result.IsFailure)
            return result;

        ContractId = contractId;
        RaiseDomainEvent(new LeadConvertedDomainEvent(Id, contractId, utcNow));

        return Result.Success();
    }

    /// <summary>Adds a note to the lead. Allowed at any status.</summary>
    /// <param name="text">The note text. Required.</param>
    /// <param name="authorId">The staff member writing the note.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The added note, or a failure if the text is empty or too long, or the author ID is empty.</returns>
    public Result<LeadNote> AddNote(string? text, Guid authorId, DateTime utcNow)
    {
        var noteResult = LeadNote.Create(text, authorId, utcNow);
        if (noteResult.IsFailure)
            return noteResult;

        _notes.Add(noteResult.Value);

        return noteResult;
    }

    private Result ChangeStatus(LeadStatus newStatus, Guid? changedByUserId, string? reason, DateTime utcNow)
    {
        var actorResult = ValidateOptionalId(changedByUserId, DomainErrors.Lead.UserIdEmpty);
        if (actorResult.IsFailure)
            return actorResult;

        _statusHistory.Add(LeadStatusChange.Record(Status, newStatus, utcNow, changedByUserId, reason));
        Status = newStatus;

        return Result.Success();
    }

    private void ApplyContact(LeadContact contact)
    {
        ContactName = contact.Name;
        Phone = contact.Phone;
        Email = contact.Email;
        DesiredCategory = contact.DesiredCategory;
        Comment = contact.Comment;
    }

    private static Result<LeadContact> ValidateContact(
        string? contactName, PhoneNumber? phone, Email? email, LicenseCategory? desiredCategory, string? comment)
    {
        var nameResult = Guard.RequiredText(contactName, MaxContactNameLength, DomainErrors.Lead.ContactNameEmpty, DomainErrors.Lead.ContactNameTooLong);
        if (nameResult.IsFailure)
            return Result.Failure<LeadContact>(nameResult.Error);

        if (phone is null && email is null)
            return Result.Failure<LeadContact>(DomainErrors.Lead.ContactRequired);

        if (desiredCategory is { } category && !Enum.IsDefined(category))
            return Result.Failure<LeadContact>(DomainErrors.Lead.CategoryNotDefined);

        var commentResult = Guard.OptionalText(comment, MaxCommentLength, DomainErrors.Lead.CommentTooLong);
        if (commentResult.IsFailure)
            return Result.Failure<LeadContact>(commentResult.Error);

        return new LeadContact(nameResult.Value, phone, email, desiredCategory, commentResult.Value);
    }

    private static Result ValidateOptionalId(Guid? id, Error emptyError) =>
        id is { } value ? Guard.NotEmpty(value, emptyError) : Result.Success();

    /// <summary>Validated contact details, passed between validation and assignment.</summary>
    private sealed record LeadContact(string Name, PhoneNumber? Phone, Email? Email, LicenseCategory? DesiredCategory, string? Comment);
}
