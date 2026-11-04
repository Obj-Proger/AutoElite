using AutoElite.Application.Users;

namespace AutoElite.Application.StudyGroups;

/// <summary>Checks that a user can be the responsible theory instructor of a group.</summary>
internal sealed class GroupInstructors(StaffMembers staffMembers)
{
    /// <summary>
    /// Checks that the user is an active theory instructor who works either for the whole school or for the group's
    /// branch.
    /// </summary>
    /// <param name="instructorId">The identifier of the user.</param>
    /// <param name="groupBranchId">The branch of the group, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result> CheckAsync(Guid instructorId, Guid? groupBranchId, CancellationToken cancellationToken) =>
        await staffMembers.CheckAsync(instructorId, UserRole.TheoryInstructor, groupBranchId, cancellationToken) switch
        {
            StaffCheck.Suitable => Result.Success(),
            StaffCheck.OfAnotherBranch => Result.Failure(ApplicationErrors.StudyGroup.InstructorOfAnotherBranch),
            _ => Result.Failure(ApplicationErrors.StudyGroup.NotATheoryInstructor)
        };
}
