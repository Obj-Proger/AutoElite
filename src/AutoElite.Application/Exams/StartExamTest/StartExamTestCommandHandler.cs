using AutoElite.Application.Contracts;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.TestSessions;

namespace AutoElite.Application.Exams.StartExamTest;

/// <summary>
/// Checks that the contract is the caller's and in the group of the exam, that the theory exam has begun and that the
/// contract has not taken it, and starts the attempt.
/// </summary>
internal sealed class StartExamTestCommandHandler(
    IRepository<Exam> examRepository,
    IStudyGroupRepository groupRepository,
    IContractRepository contractRepository,
    IRepository<TestTemplate> templateRepository,
    ITestSessionRepository sessionRepository,
    TestAttempts testAttempts,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<StartExamTestCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(StartExamTestCommand command, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null || contract.StudentId != currentUser.GetRequiredUserId())
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Contract>());

        var exam = await examRepository.GetByIdAsync(command.ExamId, cancellationToken);
        var group = exam is null ? null : await groupRepository.GetByIdAsync(exam.GroupId, cancellationToken);
        if (exam is null || group is null
            || !group.Enrollments.Any(enrollment => enrollment.IsCurrent && enrollment.ContractId == contract.Id))
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Exam>());

        var examResult = CheckExam(exam);
        if (examResult.IsFailure)
            return Result.Failure<Guid>(examResult.Error);

        if (await sessionRepository.ExistsForExamAsync(exam.Id, contract.Id, cancellationToken))
            return Result.Failure<Guid>(ApplicationErrors.Exam.AttemptAlreadyTaken);

        var template = await templateRepository.GetByIdAsync(exam.TestTemplateId!.Value, cancellationToken);
        if (template is null)
            return Result.Failure<Guid>(ApplicationErrors.NotFound<TestTemplate>());

        return await testAttempts.StartAsync(contract, template, exam.Id, cancellationToken);
    }

    private Result CheckExam(Exam exam)
    {
        if (exam.Kind != ExamKind.Theory)
            return Result.Failure(DomainErrors.Exam.WrongKind);

        if (exam.Status != ExamStatus.Scheduled)
            return Result.Failure(DomainErrors.Exam.NotScheduled);

        return timeProvider.GetUtcNow().UtcDateTime < exam.ScheduledAtUtc
            ? Result.Failure(DomainErrors.Exam.NotStarted)
            : Result.Success();
    }
}
