namespace AutoElite.Application.StudyGroups.CreateStudyGroup;

/// <summary>Checks the branch, the course and the instructor, then forms the group.</summary>
internal sealed class CreateStudyGroupCommandHandler(
    IStudyGroupRepository groupRepository,
    IRepository<Course> courseRepository,
    IRepository<Branch> branchRepository,
    GroupInstructors groupInstructors,
    ICurrentUser currentUser) : ICommandHandler<CreateStudyGroupCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        var scope = DataScope.For(currentUser);
        var branchId = command.BranchId ?? scope.BranchId;
        var branchResult = await BranchChecks.CheckAsync(branchRepository, branchId, scope, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Course>());

        var instructorResult = await groupInstructors.CheckAsync(command.TheoryInstructorId, branchId, cancellationToken);
        if (instructorResult.IsFailure)
            return Result.Failure<Guid>(instructorResult.Error);

        var groupResult = StudyGroup.Create(
            command.Name, course, command.TheoryInstructorId, branchId, command.StartDate, command.EndDate, command.Capacity);
        if (groupResult.IsFailure)
            return Result.Failure<Guid>(groupResult.Error);

        groupRepository.Add(groupResult.Value);

        return groupResult.Value.Id;
    }
}
