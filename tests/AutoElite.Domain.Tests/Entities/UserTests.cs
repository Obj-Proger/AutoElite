namespace AutoElite.Domain.Tests.Entities;

public class UserTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly FullName _fullName = FullName.Create("Ivanov", "Ivan", "Ivanovich").Value;
    private static readonly FullName _otherFullName = FullName.Create("Petrov", "Petr").Value;
    private static readonly Email _email = Email.Create("ivan@example.com").Value;
    private static readonly PhoneNumber _phone = PhoneNumber.Create("+79991234567").Value;
    private static readonly PhoneNumber _otherPhone = PhoneNumber.Create("+79990000000").Value;
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _otherBranchId = new("33333333-3333-3333-3333-333333333333");

    private static User CreateUser(PhoneNumber? phone = null, Guid? branchId = null) =>
        User.Create(_fullName, _email, phone, branchId, _now).Value;

    private static User CreateInactiveUser()
    {
        var user = CreateUser();
        user.Deactivate();

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
        result.Value.PhotoUrl.Should().BeNull();
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
    public void UpdatePhotoUrl_ValidUrl_SetsTrimmedUrl()
    {
        var user = CreateUser();

        var result = user.UpdatePhotoUrl("  https://example.com/photo.jpg  ");

        result.IsSuccess.Should().BeTrue();
        user.PhotoUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePhotoUrl_NullEmptyOrWhitespace_ReturnsPhotoUrlEmptyError(string? photoUrl)
    {
        var user = CreateUser();

        var result = user.UpdatePhotoUrl(photoUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.PhotoUrlEmpty);
        user.PhotoUrl.Should().BeNull();
    }

    [Fact]
    public void UpdatePhotoUrl_UrlExactlyAtMaximumLength_SetsUrl()
    {
        var user = CreateUser();
        var photoUrl = new string('a', 2048);

        var result = user.UpdatePhotoUrl(photoUrl);

        result.IsSuccess.Should().BeTrue();
        user.PhotoUrl.Should().Be(photoUrl);
    }

    [Fact]
    public void UpdatePhotoUrl_UrlExceedingMaximumLength_ReturnsPhotoUrlTooLongError()
    {
        var user = CreateUser();

        var result = user.UpdatePhotoUrl(new string('a', 2049));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.PhotoUrlTooLong);
        user.PhotoUrl.Should().BeNull();
    }

    [Fact]
    public void UpdatePhotoUrl_InactiveUser_ReturnsCannotModifyInactiveError()
    {
        var user = CreateInactiveUser();

        var result = user.UpdatePhotoUrl("https://example.com/photo.jpg");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
        user.PhotoUrl.Should().BeNull();
    }

    [Fact]
    public void UpdatePhotoUrl_InactiveUserAndEmptyUrl_ReportsInactiveFirst()
    {
        var user = CreateInactiveUser();

        var result = user.UpdatePhotoUrl(null);

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
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

        var result = user.Deactivate();

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_InactiveUser_ReturnsAlreadyInactiveError()
    {
        var user = CreateInactiveUser();

        var result = user.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.User.AlreadyInactive);
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
}
