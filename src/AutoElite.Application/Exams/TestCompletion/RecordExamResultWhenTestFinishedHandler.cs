using AutoElite.Application.Common.Events;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.TestSessions;

namespace AutoElite.Application.Exams.TestCompletion;

/// <summary>Records the result of a theory exam when the attempt of a student at the exam finishes.</summary>
/// <remarks>
/// The event can be delivered more than once, and the result is recorded only once; a result that the exam no longer
/// accepts, for example after the student has left the group, is not recorded.
/// </remarks>
internal sealed class RecordExamResultWhenTestFinishedHandler(
    IRepository<Exam> examRepository,
    IStudyGroupRepository groupRepository,
    ITestSessionRepository sessionRepository) : IDomainEventHandler<TestSessionFinishedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(TestSessionFinishedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ExamId is not { } examId)
            return;

        var exam = await examRepository.GetByIdAsync(examId, cancellationToken);
        var group = exam is null ? null : await groupRepository.GetByIdAsync(exam.GroupId, cancellationToken);
        var session = await sessionRepository.GetByIdAsync(domainEvent.SessionId, cancellationToken);
        if (exam is null || group is null || session is null)
            return;

        // Recording at the time the attempt finished keeps the history right even if the event is delivered late.
        exam.RecordTheoryResult(group, session, domainEvent.OccurredOnUtc);
    }
}
