using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Exams;

/// <summary>An exam loaded for an action, with its group.</summary>
/// <param name="Exam">The exam.</param>
/// <param name="Group">The group of the exam.</param>
internal sealed record ExamWithGroup(Exam Exam, StudyGroup Group);

/// <summary>
/// Loads an exam for an action of the responsible theory instructor of its group, who schedules the exams of the group;
/// for anyone else the exam is reported as not found.
/// </summary>
internal sealed class ExamAccess(IRepository<Exam> examRepository, IStudyGroupRepository groupRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the exam and its group if the caller is the responsible instructor of the group.</summary>
    /// <param name="examId">The identifier of the exam.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<ExamWithGroup>> LoadForScheduleAsync(Guid examId, CancellationToken cancellationToken)
    {
        var exam = await examRepository.GetByIdAsync(examId, cancellationToken);
        var group = exam is null ? null : await groupRepository.GetByIdAsync(exam.GroupId, cancellationToken);

        return exam is not null && group is not null && group.TheoryInstructorId == currentUser.GetRequiredUserId()
            ? new ExamWithGroup(exam, group)
            : Result.Failure<ExamWithGroup>(ApplicationErrors.NotFound<Exam>());
    }
}
