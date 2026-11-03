namespace AutoElite.Domain.Tests.Policies;

public class RoleAssignmentPolicyTests
{
    private static readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static User CreateUser() =>
        User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;

    private static User CreateInactiveUser()
    {
        var user = CreateUser();
        user.Deactivate(_now);

        return user;
    }

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.DrivingInstructor)]
    [InlineData(UserRole.TheoryInstructor)]
    [InlineData(UserRole.Student)]
    public void CanAssign_UserWithoutRoles_Succeeds(UserRole role)
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [], role);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanAssign_InactiveUser_ReturnsUserInactiveError()
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateInactiveUser(), [], UserRole.Manager);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.UserInactive);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void CanAssign_UndefinedRole_ReturnsRoleNotDefinedError(int rawRole)
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [], (UserRole)rawRole);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.RoleNotDefined);
    }

    [Fact]
    public void CanAssign_RoleAlreadyHeld_ReturnsAlreadyAssignedError()
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [UserRole.Manager], UserRole.Manager);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.AlreadyAssigned);
    }

    [Theory]
    [InlineData(UserRole.DrivingInstructor)]
    [InlineData(UserRole.TheoryInstructor)]
    public void CanAssign_StudentToInstructor_ReturnsIncompatibleRolesError(UserRole heldRole)
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [heldRole], UserRole.Student);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.IncompatibleRoles);
    }

    [Theory]
    [InlineData(UserRole.DrivingInstructor)]
    [InlineData(UserRole.TheoryInstructor)]
    public void CanAssign_InstructorToStudent_ReturnsIncompatibleRolesError(UserRole newRole)
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [UserRole.Student], newRole);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.IncompatibleRoles);
    }

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Manager)]
    public void CanAssign_StudentToManagerOrAdministrator_Succeeds(UserRole heldRole)
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [heldRole], UserRole.Student);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Manager)]
    public void CanAssign_ManagerOrAdministratorToStudent_Succeeds(UserRole newRole)
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [UserRole.Student], newRole);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanAssign_TheoryInstructorToDrivingInstructor_Succeeds()
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateUser(), [UserRole.DrivingInstructor], UserRole.TheoryInstructor);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanAssign_InactiveUserAndRoleAlreadyHeld_ReportsInactiveFirst()
    {
        var result = RoleAssignmentPolicy.CanAssign(CreateInactiveUser(), [UserRole.Manager], UserRole.Manager);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.UserInactive);
    }

    [Fact]
    public void CanRevoke_HeldRole_Succeeds()
    {
        var result = RoleAssignmentPolicy.CanRevoke([UserRole.Manager, UserRole.Student], UserRole.Manager, 1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanRevoke_RoleNotHeld_ReturnsNotAssignedError()
    {
        var result = RoleAssignmentPolicy.CanRevoke([UserRole.Manager], UserRole.Student, 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.NotAssigned);
    }

    [Fact]
    public void CanRevoke_AdministratorWhileAnotherAdministratorRemains_Succeeds()
    {
        var result = RoleAssignmentPolicy.CanRevoke([UserRole.Administrator], UserRole.Administrator, 2);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void CanRevoke_LastAdministrator_ReturnsLastAdministratorError(int activeAdministratorCount)
    {
        var result = RoleAssignmentPolicy.CanRevoke([UserRole.Administrator], UserRole.Administrator, activeAdministratorCount);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.UserRoleAssignment.LastAdministrator);
    }

    [Fact]
    public void CanRevoke_AdministratorNotHeldAndLastAdministrator_ReportsNotAssignedFirst()
    {
        var result = RoleAssignmentPolicy.CanRevoke([UserRole.Manager], UserRole.Administrator, 1);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.NotAssigned);
    }
}
