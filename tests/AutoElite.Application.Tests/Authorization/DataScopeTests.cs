using AutoElite.Application.Common.Authorization;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Authorization;

public class DataScopeTests
{
    private static readonly Guid _branchId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _otherBranchId = new("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void For_ManagerTiedToBranch_IsLimitedToThatBranch()
    {
        var manager = new FakeCurrentUser
        {
            UserId = Guid.CreateVersion7(),
            BranchId = _branchId,
            Roles = new HashSet<UserRole> { UserRole.Manager }
        };

        var scope = DataScope.For(manager);

        scope.IsSchoolWide.Should().BeFalse();
        scope.BranchId.Should().Be(_branchId);
    }

    [Fact]
    public void For_ManagerNotTiedToBranch_IsSchoolWide()
    {
        DataScope.For(FakeCurrentUser.SignedIn(UserRole.Manager)).Should().Be(DataScope.SchoolWide);
    }

    [Fact]
    public void For_AdministratorTiedToBranch_IsSchoolWide()
    {
        var administrator = new FakeCurrentUser
        {
            UserId = Guid.CreateVersion7(),
            BranchId = _branchId,
            Roles = new HashSet<UserRole> { UserRole.Manager, UserRole.Administrator }
        };

        DataScope.For(administrator).Should().Be(DataScope.SchoolWide);
    }

    [Fact]
    public void ForBranch_EmptyIdentifier_Throws()
    {
        var act = () => DataScope.ForBranch(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Includes_BranchScope_IncludesOwnBranchAndRecordsWithoutBranch()
    {
        var scope = DataScope.ForBranch(_branchId);

        scope.Includes(_branchId).Should().BeTrue();
        scope.Includes(null).Should().BeTrue();
        scope.Includes(_otherBranchId).Should().BeFalse();
    }

    [Fact]
    public void Includes_SchoolWideScope_IncludesEveryBranch()
    {
        var scope = DataScope.SchoolWide;

        scope.Includes(_branchId).Should().BeTrue();
        scope.Includes(_otherBranchId).Should().BeTrue();
        scope.Includes(null).Should().BeTrue();
    }

    [Fact]
    public void ForBranch_SameBranch_AreEqual()
    {
        DataScope.ForBranch(_branchId).Should().Be(DataScope.ForBranch(_branchId));
    }
}
