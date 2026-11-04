using AutoElite.Application.Users;

namespace AutoElite.Application.TheoryLessons;

/// <summary>Checks that a user can give a lesson of a group.</summary>
internal sealed class LessonInstructors(StaffMembers staffMembers)
{
    /// <summary>
    /// Checks that the user is an active theory instructor who works either for the whole school or for the group's branch.
    /// </summary>
    /// <param name="instructorId">The identifier of the user.</param>
    /// <param name="group">The group of the lesson.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(Guid instructorId, StudyGroup group, CancellationToken cancellationToken) =>
        await staffMembers.CheckAsync(instructorId, UserRole.TheoryInstructor, group.BranchId, cancellationToken) switch
        {
            StaffCheck.Suitable => Result.Success(),
            StaffCheck.OfAnotherBranch => Result.Failure(ApplicationErrors.StudyGroup.InstructorOfAnotherBranch),
            _ => Result.Failure(ApplicationErrors.StudyGroup.NotATheoryInstructor)
        };
}
