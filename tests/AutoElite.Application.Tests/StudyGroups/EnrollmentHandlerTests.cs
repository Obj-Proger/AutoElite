using AutoElite.Application.StudyGroups.ContractClosure;
using AutoElite.Application.StudyGroups.EnrollInStudyGroup;
using AutoElite.Application.StudyGroups.WithdrawFromStudyGroup;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.StudyGroups;

public class EnrollmentHandlerTests
{
    private readonly StudyGroupFixture _fixture = new();

    private Task<Result<Guid>> Enroll(Guid groupId, Guid contractId, FakeCurrentUser? caller = null)
    {
        var user = caller ?? TestData.Administrator();

        return new EnrollInStudyGroupCommandHandler(_fixture.CreateGroupAccess(user), _fixture.CreateContractAccess(user), TestData.Clock())
            .Handle(new EnrollInStudyGroupCommand(groupId, contractId), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Enroll_ActiveContractOfTheCourse_EnrollsTheStudent()
    {
        var group = _fixture.SeedGroup();
        var contract = _fixture.SeedContract();

        var result = await Enroll(group.Id, contract.Id);

        var enrollment = group.Enrollments.Should().ContainSingle().Which;
        enrollment.Id.Should().Be(result.Value);
        enrollment.ContractId.Should().Be(contract.Id);
        enrollment.StudentId.Should().Be(contract.StudentId);
        group.CurrentMemberCount.Should().Be(1);
    }

    [Fact]
    public async Task Enroll_FullGroup_ReturnsGroupFull()
    {
        var group = _fixture.SeedGroup(capacity: 1);
        await Enroll(group.Id, _fixture.SeedContract().Id);

        var result = await Enroll(group.Id, _fixture.SeedContract().Id);

        result.Error.Should().Be(DomainErrors.StudyGroup.GroupFull);
    }

    [Fact]
    public async Task Enroll_ContractOfAnotherCourse_ReturnsCourseMismatch()
    {
        var group = _fixture.SeedGroup();
        var otherCourse = TestData.NewCourse("Category A");
        var contract = _fixture.SeedContract(course: otherCourse);

        var result = await Enroll(group.Id, contract.Id);

        result.Error.Should().Be(DomainErrors.StudyGroup.CourseMismatch);
    }

    [Fact]
    public async Task Enroll_ContractOfAnotherBranch_ReturnsContractOfAnotherBranch()
    {
        var group = _fixture.SeedGroup(Guid.CreateVersion7());
        var contract = _fixture.SeedContract(Guid.CreateVersion7());

        var result = await Enroll(group.Id, contract.Id);

        result.Error.Should().Be(ApplicationErrors.StudyGroup.ContractOfAnotherBranch);
        group.Enrollments.Should().BeEmpty();
    }

    [Fact]
    public async Task Enroll_ContractOutsideTheManagersScope_ReturnsContractNotFound()
    {
        var branchId = Guid.CreateVersion7();
        var group = _fixture.SeedGroup(branchId);
        var contract = _fixture.SeedContract(Guid.CreateVersion7());

        var result = await Enroll(group.Id, contract.Id, TestData.Manager(branchId));

        result.Error.Should().Be(ApplicationErrors.NotFound<Contract>());
    }

    [Fact]
    public async Task Withdraw_CurrentMember_EndsTheEnrollment()
    {
        var group = _fixture.SeedGroup();
        var contract = _fixture.SeedContract();
        await Enroll(group.Id, contract.Id);

        var result = await new WithdrawFromStudyGroupCommandHandler(_fixture.CreateGroupAccess(TestData.Administrator()), TestData.Clock())
            .Handle(new WithdrawFromStudyGroupCommand(group.Id, contract.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        group.CurrentMemberCount.Should().Be(0);
        group.Enrollments.Should().ContainSingle().Which.WithdrawnAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task ContractClosed_WithdrawsFromEveryGroupAtTheTimeOfClosing()
    {
        var first = _fixture.SeedGroup();
        var second = _fixture.SeedGroup();
        var contract = _fixture.SeedContract();
        await Enroll(first.Id, contract.Id);
        await Enroll(second.Id, contract.Id);
        second.Deactivate();
        var closedAt = TestData.Now.AddDays(30);

        await new WithdrawFromGroupsWhenContractClosedHandler(_fixture.Groups).Handle(
            new ContractClosedDomainEvent(contract.Id, contract.StudentId, ContractStatus.Terminated, closedAt),
            TestContext.Current.CancellationToken);

        first.CurrentMemberCount.Should().Be(0);
        second.CurrentMemberCount.Should().Be(0);
        first.Enrollments.Single().WithdrawnAtUtc.Should().Be(closedAt);
    }
}
