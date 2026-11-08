using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// Represents a user account's business profile: identity, contact details,
/// branch affiliation, and activation status.
/// </summary>
/// <remarks>
/// Authentication data (password hash, email confirmation, security stamp, refresh tokens)
/// is intentionally not part of this entity. It lives in the infrastructure layer and is
/// linked to this entity by the shared <see cref="BaseEntity.Id"/>. Roles are assigned separately.
/// </remarks>
public sealed class User : AuditableEntity
{
    /// <summary>Gets the user's full name.</summary>
    public FullName FullName { get; private set; } = null!;

    /// <summary>Gets the user's email address, which also serves as the login identifier.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>Gets the user's phone number, or <see langword="null"/> if not provided.</summary>
    public PhoneNumber? Phone { get; private set; }

    /// <summary>
    /// Gets the identifier of the document that is the user's profile photo, or <see langword="null"/> if not set.
    /// Checking that the document is the user's image is up to the caller.
    /// </summary>
    public Guid? PhotoDocumentId { get; private set; }

    /// <summary>
    /// Gets the identifier of the branch the user is assigned to,
    /// or <see langword="null"/> if the user is not tied to a specific branch.
    /// </summary>
    public Guid? BranchId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the account is active.
    /// Deactivated users are kept for history but cannot be modified.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the language notifications and emails are written in.</summary>
    public Language PreferredLanguage { get; private set; }

    private User(Guid id, FullName fullName, Email email, PhoneNumber? phone, Guid? branchId, Language preferredLanguage) : base(id)
    {
        FullName = fullName;
        Email = email;
        Phone = phone;
        BranchId = branchId;
        PreferredLanguage = preferredLanguage;
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private User()
    {
    }

    /// <summary>Creates a new active user and raises <see cref="UserRegisteredDomainEvent"/>.</summary>
    /// <param name="fullName">The user's validated full name.</param>
    /// <param name="email">The user's validated email address.</param>
    /// <param name="phone">The user's validated phone number, if any.</param>
    /// <param name="branchId">The branch to assign the user to, or <see langword="null"/> for none.</param>
    /// <param name="utcNow">The current UTC time, used as the event timestamp.</param>
    /// <param name="preferredLanguage">The language for notifications and emails; English unless given.</param>
    /// <returns>The created user, or a failure if <paramref name="branchId"/> is an empty GUID or the language is not defined.</returns>
    public static Result<User> Create(
        FullName fullName, Email email, PhoneNumber? phone, Guid? branchId, DateTime utcNow, Language preferredLanguage = Language.English)
    {
        var branchIdResult = ValidateBranchId(branchId);
        if (branchIdResult.IsFailure)
            return Result.Failure<User>(branchIdResult.Error);

        var languageResult = Guard.DefinedEnum(preferredLanguage, DomainErrors.User.LanguageNotDefined);
        if (languageResult.IsFailure)
            return Result.Failure<User>(languageResult.Error);

        var user = new User(Guid.CreateVersion7(), fullName, email, phone, branchId, preferredLanguage);

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email.Value, utcNow));

        return user;
    }

    /// <summary>
    /// Updates the user's full name and phone number.
    /// Passing <see langword="null"/> for <paramref name="phone"/> removes the phone number.
    /// </summary>
    /// <returns>A failure if the user is inactive.</returns>
    public Result UpdateProfile(FullName fullName, PhoneNumber? phone)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        FullName = fullName;
        Phone = phone;

        return Result.Success();
    }

    /// <summary>
    /// Updates the user's email address and raises <see cref="UserEmailChangedDomainEvent"/>.
    /// Uniqueness of the address among users is checked by the caller.
    /// </summary>
    /// <param name="email">The new validated email address.</param>
    /// <param name="utcNow">The current UTC time, used as the event timestamp.</param>
    /// <returns>A failure if the user is inactive or the address is unchanged.</returns>
    public Result ChangeEmail(Email email, DateTime utcNow)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        if (email == Email)
            return Result.Failure(DomainErrors.User.EmailUnchanged);

        var previousEmail = Email;
        Email = email;

        RaiseDomainEvent(new UserEmailChangedDomainEvent(Id, previousEmail.Value, email.Value, utcNow));

        return Result.Success();
    }

    /// <summary>Makes a document the user's profile photo. Use <see cref="RemovePhoto"/> to clear it.</summary>
    /// <param name="documentId">The document with the photo.</param>
    /// <returns>A failure if the user is inactive or the identifier is empty.</returns>
    public Result SetPhoto(Guid documentId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        var documentResult = Guard.NotEmpty(documentId, DomainErrors.User.PhotoDocumentIdEmpty);
        if (documentResult.IsFailure)
            return Result.Failure(documentResult.Error);

        PhotoDocumentId = documentId;

        return Result.Success();
    }

    /// <summary>Removes the user's profile photo.</summary>
    /// <returns>A failure if the user is inactive or has no photo.</returns>
    public Result RemovePhoto()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        if (PhotoDocumentId is null)
            return Result.Failure(DomainErrors.User.PhotoNotSet);

        PhotoDocumentId = null;

        return Result.Success();
    }

    /// <summary>
    /// Assigns the user to a branch.
    /// Passing <see langword="null"/> removes the current branch assignment.
    /// </summary>
    /// <returns>A failure if the user is inactive, or <paramref name="branchId"/> is an empty GUID.</returns>
    public Result AssignBranch(Guid? branchId)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        var branchIdResult = ValidateBranchId(branchId);
        if (branchIdResult.IsFailure)
            return Result.Failure(branchIdResult.Error);

        BranchId = branchId;

        return Result.Success();
    }

    /// <summary>Changes the language notifications and emails are written in.</summary>
    /// <returns>A failure if the user is inactive or the language is not defined.</returns>
    public Result ChangeLanguage(Language language)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.CannotModifyInactive);

        var languageResult = Guard.DefinedEnum(language, DomainErrors.User.LanguageNotDefined);
        if (languageResult.IsFailure)
            return Result.Failure(languageResult.Error);

        PreferredLanguage = language;

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the user without deleting any data and raises <see cref="UserDeactivatedDomainEvent"/>.
    /// </summary>
    /// <param name="utcNow">The current UTC time, used as the event timestamp.</param>
    /// <returns>A failure if the user is already inactive.</returns>
    public Result Deactivate(DateTime utcNow)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.User.AlreadyInactive);

        IsActive = false;

        RaiseDomainEvent(new UserDeactivatedDomainEvent(Id, utcNow));

        return Result.Success();
    }

    /// <summary>Reactivates a previously deactivated user.</summary>
    /// <returns>A failure if the user is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.User.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    /// <summary>
    /// Validates an optional branch identifier.
    /// A <see langword="null"/> value is valid (the user is not tied to a specific branch);
    /// a non-null value must not be an empty GUID.
    /// </summary>
    private static Result ValidateBranchId(Guid? branchId)
    {
        if (branchId is null)
            return Result.Success();

        return Guard.NotEmpty(branchId.Value, DomainErrors.User.BranchIdEmpty);
    }
}
