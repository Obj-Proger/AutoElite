using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StaffTasks;
using AutoElite.Application.StaffTasks.GetMyStaffTasks;
using AutoElite.Application.StaffTasks.GetStaffTask;
using AutoElite.Application.StaffTasks.GetStaffTasks;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.StaffTasks;

public class StaffTaskQueryHandlerTests
{
    private static readonly Guid _branchId = Guid.CreateVersion7();

    private readonly FakeStaffTaskQueries _queries = new();
    private readonly FakeCurrentUser _instructor = FakeCurrentUser.SignedIn(UserRole.DrivingInstructor);

    private StaffTaskDetails SeedDetails(Guid assigneeId, Guid? assigneeBranchId)
    {
        var details = new StaffTaskDetails(
            Guid.CreateVersion7(), "Call the lead back", null, assigneeId, "Ivanov Ivan", assigneeBranchId, TestData.Now.AddDays(1),
            StaffTaskStatus.New, TaskRecurrence.None, null, null, TestData.Now, null);
        _queries.Seed(details);

        return details;
    }

    private Task<Result<StaffTaskDetails>> GetCard(StaffTaskDetails details, FakeCurrentUser caller) =>
        new GetStaffTaskQueryHandler(_queries, caller).Handle(new GetStaffTaskQuery(details.Id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetMy_PassesTheCallerAndTheCurrentTimeForOverdue()
    {
        await new GetMyStaffTasksQueryHandler(_queries, _instructor, TestData.Clock())
            .Handle(new GetMyStaffTasksQuery(StaffTaskStatus.New, OverdueOnly: true, 2, 10), TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be((
            new StaffTaskListFilter(_instructor.UserId, StaffTaskStatus.New, TestData.Now, null),
            DataScope.SchoolWide,
            new PageRequest(2, 10)));
    }

    [Fact]
    public async Task GetStaffTasks_Manager_LimitsTheListToTheManagersBranchAndPassesTheSubject()
    {
        var manager = TestData.Manager(_branchId);
        var leadId = Guid.CreateVersion7();

        await new GetStaffTasksQueryHandler(_queries, manager, TestData.Clock())
            .Handle(new GetStaffTasksQuery(SubjectKind: EntityKind.Lead, SubjectId: leadId), TestContext.Current.CancellationToken);

        var (filter, scope, _) = _queries.LastListCall!.Value;
        scope.Should().Be(DataScope.ForBranch(_branchId));
        filter.Subject.Should().Be(EntityReference.Create(EntityKind.Lead, leadId).Value);
        filter.OverdueBeforeUtc.Should().BeNull();
    }

    [Fact]
    public void GetStaffTasksValidator_SubjectIdWithoutKind_ReportsSubjectIncomplete()
    {
        var result = new GetStaffTasksQueryValidator().Validate(new GetStaffTasksQuery(SubjectId: Guid.CreateVersion7()));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.StaffTask.SubjectIncomplete.Code);
    }

    [Fact]
    public async Task GetCard_OwnTask_ReturnsIt()
    {
        var details = SeedDetails(_instructor.UserId!.Value, _branchId);

        var result = await GetCard(details, _instructor);

        result.Value.Should().Be(details);
    }

    [Fact]
    public async Task GetCard_TaskOfAnotherInstructor_ReturnsNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7(), _branchId);

        var result = await GetCard(details, _instructor);

        result.Error.Should().Be(ApplicationErrors.NotFound<StaffTask>());
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task GetCard_Manager_SeesOnlyTasksOfTheirBranchStaff(bool sameBranch, bool visible)
    {
        var details = SeedDetails(Guid.CreateVersion7(), sameBranch ? _branchId : Guid.CreateVersion7());

        var result = await GetCard(details, TestData.Manager(_branchId));

        result.IsSuccess.Should().Be(visible);
    }
}
