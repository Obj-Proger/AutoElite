using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Exams.ScheduleExam;

/// <summary>Checks that the caller is the responsible instructor of the group, loads the template and schedules the exam.</summary>
internal sealed class ScheduleExamCommandHandler(
    IRepository<Exam> examRepository,
    IStudyGroupRepository groupRepository,
    IRepository<TestTemplate> templateRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ScheduleExamCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(ScheduleExamCommand command, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group is null || group.TheoryInstructorId != currentUser.GetRequiredUserId())
            return Result.Failure<Guid>(ApplicationErrors.NotFound<StudyGroup>());

        TestTemplate? template = null;
        if (command.TestTemplateId is { } templateId)
        {
            template = await templateRepository.GetByIdAsync(templateId, cancellationToken);
            if (template is null)
                return Result.Failure<Guid>(ApplicationErrors.NotFound<TestTemplate>());
        }

        var examResult = Exam.Schedule(group, command.Kind, template, command.ScheduledAtUtc, timeProvider.GetUtcNow().UtcDateTime);
        if (examResult.IsFailure)
            return Result.Failure<Guid>(examResult.Error);

        examRepository.Add(examResult.Value);

        return examResult.Value.Id;
    }
}
