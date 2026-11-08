namespace AutoElite.Domain.Tests.Entities;

public class UserTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly FullName _fullName = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;
    private static readonly FullName _otherFullName = FullName.Create("Petrov", "Petr").Value;
    private static readonly Email _email = Email.Create("ivan@example.com").Value;
    private static readonly Email _otherEmail = Email.Create("petr@example.com").Value;
    private static readonly PhoneNumber _phone = PhoneNumber.Create("+79991234567").Value;
    private static readonly PhoneNumber _otherPhone = PhoneNumber.Create("+79990000000").Value;
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _otherBranchId = new("33333333-3333-3333-3333-333333333333");
    private static readonly DateTime _later = new(2026, 1, 2, 9, 30, 0, DateTimeKind.Utc);

    private static User CreateUser(PhoneNumber? phone = null, Guid? branchId = null) =>
        User.Create(_fullName, _email, phone, branchId, _now).Value;

    private static User CreateInactiveUser()
    {
        var user = CreateUser();
        user.Deactivate(_now);

        return user;
    }

    [Fact]
    public void Create_ValidData_ReturnsActiveUserWithGivenValues()
    {
        var result = User.Create(_fullName, _email, _phone, _branchId, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be(_fullName);
        result.Value.Email.Should().Be(_email);
        result.Value.Phone.Should().Be(_phone);
        result.Value.BranchId.Should().Be(_branchId);
        result.Value.PhotoDocumentId.Should().BeNull();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutOptionalData_ReturnsUserWithoutPhoneAndBranch()
    {
        var result = User.Create(_fullName, _email, null, null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone.Should().BeNull();
        result.Value.BranchId.Should().BeNull();
    }

    [Fact]
    public void Create_ValidData_GeneratesNonEmptyId()
    {
        var result = User.Create(_fullName, _email, null, null, _now);

        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ValidData_RaisesUserRegisteredEvent()
    {
        var user = User.Create(_fullName, _email, null, null, _now).Value;

        user.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new UserRegisteredDomainEvent(user.Id, "ivan@example.com", _now));
    }

    [Fact]
    public void Create_ValidData_LeavesAuditDataEmptyUntilSaved()
    {
        var user = User.Create(_fullName, _email, null, null, _now).Value;

        user.Should().BeAssignableTo<IAuditable>();
        user.CreatedAtUtc.Should().Be(default(DateTime));
        user.CreatedByUserId.Should().BeNull();
        user.ModifiedAtUtc.Should().BeNull();
        user.ModifiedByUserId.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyBranchId_ReturnsBranchIdEmptyError()
    {
        var result = User.Create(_fullName, _email, null, Guid.Empty, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.BranchIdEmpty);
    }

    [Fact]
    public void UpdateProfile_ActiveUser_ChangesNameAndPhone()
    {
        var user = CreateUser(_phone);

        var result = user.UpdateProfile(_otherFullName, _otherPhone);

        result.IsSuccess.Should().BeTrue();
        user.FullName.Should().Be(_otherFullName);
        user.Phone.Should().Be(_otherPhone);
    }

    [Fact]
    public void UpdateProfile_NullPhone_RemovesPhone()
    {
        var user = CreateUser(_phone);

        var result = user.UpdateProfile(_fullName, null);

        result.IsSuccess.Should().BeTrue();
        user.Phone.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_InactiveUser_ReturnsCannotModifyInactiveErrorAndKeepsData()
    {
        var user = CreateInactiveUser();

        var result = user.UpdateProfile(_otherFullName, _otherPhone);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
        user.FullName.Should().Be(_fullName);
        user.Phone.Should().BeNull();
    }

    [Fact]
    public void ChangeEmail_NewAddress_ChangesEmail()
    {
        var user = CreateUser();

        var result = user.ChangeEmail(_otherEmail, _later);

        result.IsSuccess.Should().BeTrue();
        user.Email.Should().Be(_otherEmail);
    }

    [Fact]
    public void ChangeEmail_NewAddress_RaisesUserEmailChangedEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.ChangeEmail(_otherEmail, _later);

        user.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new UserEmailChangedDomainEvent(user.Id, "ivan@example.com", "petr@example.com", _later));
    }

    [Fact]
    public void ChangeEmail_SameAddress_ReturnsEmailUnchangedErrorWithoutEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        var result = user.ChangeEmail(Email.Create("IVAN@example.com").Value, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.EmailUnchanged);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeEmail_InactiveUser_ReturnsCannotModifyInactiveErrorAndKeepsEmail()
    {
        var user = CreateInactiveUser();

        var result = user.ChangeEmail(_otherEmail, _later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
        user.Email.Should().Be(_email);
    }

    [Fact]
    public void ChangeEmail_InactiveUserAndSameAddress_ReportsInactiveFirst()
    {
        var user = CreateInactiveUser();

        var result = user.ChangeEmail(_email, _later);

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
    }

    [Fact]
    public void SetPhoto_Document_SetsIt()
    {
        var user = CreateUser();
        var documentId = Guid.CreateVersion7();

        var result = user.SetPhoto(documentId);

        result.IsSuccess.Should().BeTrue();
        user.PhotoDocumentId.Should().Be(documentId);
    }

    [Fact]
    public void SetPhoto_EmptyIdentifier_ReturnsPhotoDocumentIdEmptyError()
    {
        var user = CreateUser();

        var result = user.SetPhoto(Guid.Empty);

        result.Error.Should().Be(DomainErrors.User.PhotoDocumentIdEmpty);
        user.PhotoDocumentId.Should().BeNull();
    }

    [Fact]
    public void SetPhoto_InactiveUser_ReturnsCannotModifyInactiveError()
    {
        var user = CreateUser();
        user.Deactivate(_now);

        var result = user.SetPhoto(Guid.CreateVersion7());

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
        user.PhotoDocumentId.Should().BeNull();
    }

    [Fact]
    public void RemovePhoto_UserWithPhoto_ClearsPhoto()
    {
        var user = CreateUser();
        user.SetPhoto(Guid.CreateVersion7());

        var result = user.RemovePhoto();

        result.IsSuccess.Should().BeTrue();
        user.PhotoDocumentId.Should().BeNull();
    }

    [Fact]
    public void RemovePhoto_UserWithoutPhoto_ReturnsPhotoNotSetError()
    {
        var user = CreateUser();

        var result = user.RemovePhoto();

        result.Error.Should().Be(DomainErrors.User.PhotoNotSet);
    }

    [Fact]
    public void RemovePhoto_InactiveUserWithPhoto_ReturnsCannotModifyInactiveErrorAndKeepsPhoto()
    {
        var user = CreateUser();
        var documentId = Guid.CreateVersion7();
        user.SetPhoto(documentId);
        user.Deactivate(_now);

        var result = user.RemovePhoto();

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
        user.PhotoDocumentId.Should().Be(documentId);
    }

    [Fact]
    public void AssignBranch_ValidId_SetsBranch()
    {
        var user = CreateUser(branchId: _branchId);

        var result = user.AssignBranch(_otherBranchId);

        result.IsSuccess.Should().BeTrue();
        user.BranchId.Should().Be(_otherBranchId);
    }

    [Fact]
    public void AssignBranch_Null_RemovesBranch()
    {
        var user = CreateUser(branchId: _branchId);

        var result = user.AssignBranch(null);

        result.IsSuccess.Should().BeTrue();
        user.BranchId.Should().BeNull();
    }

    [Fact]
    public void AssignBranch_EmptyGuid_ReturnsBranchIdEmptyErrorAndKeepsBranch()
    {
        var user = CreateUser(branchId: _branchId);

        var result = user.AssignBranch(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.BranchIdEmpty);
        user.BranchId.Should().Be(_branchId);
    }

    [Fact]
    public void AssignBranch_InactiveUser_ReturnsCannotModifyInactiveError()
    {
        var user = CreateInactiveUser();

        var result = user.AssignBranch(_branchId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
        user.BranchId.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ActiveUser_BecomesInactive()
    {
        var user = CreateUser();

        var result = user.Deactivate(_now);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ActiveUser_RaisesUserDeactivatedEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.Deactivate(_later);

        user.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new UserDeactivatedDomainEvent(user.Id, _later));
    }

    [Fact]
    public void Deactivate_InactiveUser_ReturnsAlreadyInactiveError()
    {
        var user = CreateInactiveUser();
        user.ClearDomainEvents();

        var result = user.Deactivate(_later);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.AlreadyInactive);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reactivate_InactiveUser_BecomesActive()
    {
        var user = CreateInactiveUser();

        var result = user.Reactivate();

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_ActiveUser_ReturnsAlreadyActiveError()
    {
        var user = CreateUser();

        var result = user.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.AlreadyActive);
    }

    [Fact]
    public void Reactivate_AfterDeactivate_AllowsModificationAgain()
    {
        var user = CreateInactiveUser();
        user.Reactivate();

        var result = user.UpdateProfile(_otherFullName, _otherPhone);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutLanguage_UsesEnglish()
    {
        var user = User.Create(_fullName, _email, null, null, _now).Value;

        user.PreferredLanguage.Should().Be(Language.English);
    }

    [Fact]
    public void Create_WithLanguage_UsesIt()
    {
        var user = User.Create(_fullName, _email, null, null, _now, Language.Russian).Value;

        user.PreferredLanguage.Should().Be(Language.Russian);
    }

    [Fact]
    public void Create_UndefinedLanguage_ReturnsLanguageNotDefinedError()
    {
        var result = User.Create(_fullName, _email, null, null, _now, (Language)9);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.LanguageNotDefined);
    }

    [Fact]
    public void ChangeLanguage_ActiveUser_ChangesLanguage()
    {
        var user = CreateUser();

        var result = user.ChangeLanguage(Language.Russian);

        result.IsSuccess.Should().BeTrue();
        user.PreferredLanguage.Should().Be(Language.Russian);
    }

    [Fact]
    public void ChangeLanguage_UndefinedLanguage_ReturnsLanguageNotDefinedError()
    {
        var user = CreateUser();

        var result = user.ChangeLanguage((Language)9);

        result.Error.Should().Be(DomainErrors.User.LanguageNotDefined);
        user.PreferredLanguage.Should().Be(Language.English);
    }

    [Fact]
    public void ChangeLanguage_InactiveUser_ReturnsCannotModifyInactiveError()
    {
        var user = CreateInactiveUser();

        var result = user.ChangeLanguage(Language.English);

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
    }
}
