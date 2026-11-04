using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.StudyGroups.GetStudyGroup;
using AutoElite.Application.StudyGroups.GetStudyGroups;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.StudyGroups;

public class StudyGroupQueryHandlerTests
{
    private readonly FakeStudyGroupQueries _queries = new();

    private static FakeCurrentUser Instructor(Guid userId) =>
        new() { UserId = userId, Roles = new HashSet<UserRole> { UserRole.TheoryInstructor } };

    private Task<Result<PagedList<StudyGroupListItem>>> GetGroups(GetStudyGroupsQuery query, FakeCurrentUser caller) =>
        new GetStudyGroupsQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    private Task<Result<StudyGroupDetails>> GetGroup(Guid groupId, FakeCurrentUser caller) =>
        new GetStudyGroupQueryHandler(_queries, caller).Handle(new GetStudyGroupQuery(groupId), TestContext.Current.CancellationToken);

    private StudyGroupDetails SeedDetails(Guid instructorId, Guid? branchId = null)
    {
        var details = new StudyGroupDetails(
            Guid.CreateVersion7(), "B-2026-03", Guid.CreateVersion7(), "Category B", LicenseCategory.B, instructorId,
            "Sidorova Maria", branchId, null, new DateOnly(2026, 3, 10), new DateOnly(2026, 7, 31), 20, true, []);
        _queries.Seed(details);

        return details;
    }

    [Fact]
    public async Task GetGroups_Manager_PassesTheFiltersWithinTheBranchScope()
    {
        var branchId = Guid.CreateVersion7();
        var courseId = Guid.CreateVersion7();

        await GetGroups(new GetStudyGroupsQuery(" B-2026 ", courseId, null, branchId, true), TestData.Manager(branchId));

        _queries.LastListCall!.Value.Filter.Should().Be(new StudyGroupListFilter("B-2026", courseId, null, branchId, true));
        _queries.LastListCall.Value.Scope.Should().Be(DataScope.ForBranch(branchId));
    }

    [Fact]
    public async Task GetGroups_TheoryInstructor_ReadsOnlyOwnGroups()
    {
        var instructorId = Guid.CreateVersion7();

        await GetGroups(new GetStudyGroupsQuery(), Instructor(instructorId));

        _queries.LastListCall!.Value.Filter.TheoryInstructorId.Should().Be(instructorId);
    }

    [Fact]
    public async Task GetGroups_TheoryInstructorAsksForAnotherInstructor_ReturnsAnEmptyPageWithoutReading()
    {
        await GetGroups(new GetStudyGroupsQuery(TheoryInstructorId: Guid.CreateVersion7()), Instructor(Guid.CreateVersion7()));

        _queries.LastListCall.Should().BeNull();
    }

    [Fact]
    public async Task GetGroup_OwnGroupOfTheInstructor_ReadsTheCard()
    {
        var instructorId = Guid.CreateVersion7();
        var details = SeedDetails(instructorId, Guid.CreateVersion7());

        var result = await GetGroup(details.Id, Instructor(instructorId));

        result.Value.Should().BeSameAs(details);
    }

    [Fact]
    public async Task GetGroup_GroupOfAnotherInstructor_ReturnsStudyGroupNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7());

        var result = await GetGroup(details.Id, Instructor(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }

    [Fact]
    public async Task GetGroup_ManagerOfAnotherBranch_ReturnsStudyGroupNotFound()
    {
        var details = SeedDetails(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await GetGroup(details.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
    }
}
