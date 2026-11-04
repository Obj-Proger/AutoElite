using AutoElite.Application.StudyGroups.CreateStudyGroup;
using AutoElite.Application.StudyGroups.DeactivateStudyGroup;
using AutoElite.Application.StudyGroups.UpdateStudyGroup;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.StudyGroups;

public class StudyGroupCommandHandlerTests
{
    private readonly StudyGroupFixture _fixture = new();

    private Task<Result<Guid>> Create(CreateStudyGroupCommand command, FakeCurrentUser? caller = null)
    {
        var user = caller ?? TestData.Administrator();

        return new CreateStudyGroupCommandHandler(
                _fixture.Groups, _fixture.Courses, _fixture.Branches, _fixture.CreateGroupInstructors(), user)
            .Handle(command, TestContext.Current.CancellationToken);
    }

    private Task<Result> Update(UpdateStudyGroupCommand command) =>
        new UpdateStudyGroupCommandHandler(_fixture.CreateGroupAccess(TestData.Administrator()), _fixture.CreateGroupInstructors())
            .Handle(command, TestContext.Current.CancellationToken);

    private CreateStudyGroupCommand CreateCommand(Guid instructorId, Guid? branchId = null) =>
        new("B-2026-03", _fixture.Course.Id, instructorId, StudyGroupFixture.Start, StudyGroupFixture.End, 25, branchId);

    [Fact]
    public async Task Create_ByManager_FormsTheGroupInTheManagersBranch()
    {
        var branch = _fixture.SeedBranch();
        var instructor = _fixture.SeedInstructor(branch.Id);

        var result = await Create(CreateCommand(instructor.Id), TestData.Manager(branch.Id));

        var group = _fixture.Groups.Items.Should().ContainSingle().Which;
        group.Id.Should().Be(result.Value);
        group.BranchId.Should().Be(branch.Id);
        group.CourseId.Should().Be(_fixture.Course.Id);
        group.TheoryInstructorId.Should().Be(instructor.Id);
        group.Capacity.Should().Be(25);
    }

    [Fact]
    public async Task Create_InstructorOfAnotherBranch_ReturnsInstructorOfAnotherBranch()
    {
        var central = _fixture.SeedBranch("Central");
        var north = _fixture.SeedBranch("North");
        var instructor = _fixture.SeedInstructor(north.Id);

        var result = await Create(CreateCommand(instructor.Id, central.Id));

        result.Error.Should().Be(ApplicationErrors.StudyGroup.InstructorOfAnotherBranch);
    }

    [Fact]
    public async Task Create_DrivingInstructor_ReturnsNotATheoryInstructor()
    {
        var instructor = _fixture.SeedInstructor(role: UserRole.DrivingInstructor);

        var result = await Create(CreateCommand(instructor.Id));

        result.Error.Should().Be(ApplicationErrors.StudyGroup.NotATheoryInstructor);
        _fixture.Groups.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_UnknownCourse_ReturnsCourseNotFound()
    {
        var instructor = _fixture.SeedInstructor();

        var result = await Create(CreateCommand(instructor.Id) with { CourseId = Guid.CreateVersion7() });

        result.Error.Should().Be(ApplicationErrors.NotFound<Course>());
    }

    [Fact]
    public async Task Create_CapacityOutOfRange_ReturnsTheDomainError()
    {
        var instructor = _fixture.SeedInstructor();

        var result = await Create(CreateCommand(instructor.Id) with { Capacity = 0 });

        result.Error.Should().Be(DomainErrors.StudyGroup.CapacityOutOfRange);
    }

    [Fact]
    public async Task Update_SameInstructor_AppliesWithoutCheckingTheInstructorAgain()
    {
        var group = _fixture.SeedGroup();
        _fixture.Users.Items.Single(user => user.Id == group.TheoryInstructorId).Deactivate(TestData.Now);

        var result = await Update(new UpdateStudyGroupCommand(
            group.Id, "B-2026-04", group.TheoryInstructorId, StudyGroupFixture.Start, StudyGroupFixture.End.AddDays(7), 30));

        result.IsSuccess.Should().BeTrue();
        group.Name.Should().Be("B-2026-04");
        group.Capacity.Should().Be(30);
    }

    [Fact]
    public async Task Update_NewInstructorWhoIsNotTheoryInstructor_ReturnsNotATheoryInstructor()
    {
        var group = _fixture.SeedGroup();
        var manager = _fixture.SeedInstructor(email: "manager@example.com", role: UserRole.Manager);

        var result = await Update(new UpdateStudyGroupCommand(
            group.Id, "B-2026-03", manager.Id, StudyGroupFixture.Start, StudyGroupFixture.End, 20));

        result.Error.Should().Be(ApplicationErrors.StudyGroup.NotATheoryInstructor);
    }

    [Fact]
    public async Task Deactivate_GroupOfAnotherBranch_ReturnsStudyGroupNotFound()
    {
        var group = _fixture.SeedGroup(Guid.CreateVersion7());

        var result = await new DeactivateStudyGroupCommandHandler(_fixture.CreateGroupAccess(TestData.Manager(Guid.CreateVersion7())))
            .Handle(new DeactivateStudyGroupCommand(group.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<StudyGroup>());
        group.IsActive.Should().BeTrue();
    }
}
