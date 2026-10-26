namespace AutoElite.Domain.Tests.Entities;

public class UserRoleAssignmentTests
{
    private static readonly Guid _userId = new("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.DrivingInstructor)]
    [InlineData(UserRole.TheoryInstructor)]
    [InlineData(UserRole.Student)]
    public void Create_ValidData_ReturnsAssignmentWithGivenValues(UserRole role)
    {
        var result = UserRoleAssignment.Create(_userId, role);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(_userId);
        result.Value.Role.Should().Be(role);
    }

    [Fact]
    public void Create_ValidData_GeneratesNonEmptyId()
    {
        var result = UserRoleAssignment.Create(_userId, UserRole.Student);

        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_EmptyUserId_ReturnsUserIdEmptyError()
    {
        var result = UserRoleAssignment.Create(Guid.Empty, UserRole.Student);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.UserIdEmpty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Create_UndefinedRole_ReturnsRoleNotDefinedError(int rawRole)
    {
        var result = UserRoleAssignment.Create(_userId, (UserRole)rawRole);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.RoleNotDefined);
    }

    [Fact]
    public void Create_EmptyUserIdAndUndefinedRole_ReportsUserIdFirst()
    {
        var result = UserRoleAssignment.Create(Guid.Empty, (UserRole)99);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.UserIdEmpty);
    }
}
