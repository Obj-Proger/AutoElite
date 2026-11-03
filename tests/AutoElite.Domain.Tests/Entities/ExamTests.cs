namespace AutoElite.Domain.Tests.Entities;

public class ExamTests
{
    private static readonly DateTime _now = new(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _examStart = new(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _duringExam = _examStart.AddMinutes(30);
    private static readonly DateOnly _start = new(2026, 2, 1);
    private static readonly DateOnly _end = new(2026, 5, 31);
    private static readonly Guid _instructorId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _managerId = new("44444444-4444-4444-4444-444444444444");
    private static readonly Currency _rub = Currency.Create("RUB").Value;

    private readonly Course _course;
    private readonly Guid _topicId;
    private readonly StudyGroup _group;
    private readonly TestTemplate _template;
    private readonly Question[] _questions;

    public ExamTests()
    {
        _course = Course.Create("Category B", LicenseCategory.B, Money.Create(45000m, _rub).Value, 134, 56, null).Value;
        _topicId = _course.AddTopic("Road signs", null).Value.Id;
        _group = StudyGroup.Create("B-2026-02", _course, _instructorId, null, _start, _end, 20).Value;
        var rules = TestRules.Create(3, TimeSpan.FromMinutes(5), 1, null, 0, TimeSpan.Zero).Value;
        _template = TestTemplate.Create("Final test", LicenseCategory.B, rules, [TestBlock.Create(3, []).Value], null).Value;
        _questions = [.. Enumerable.Range(1, 3).Select(number => Question.Create(
            $"Question {number}", null, null, _topicId, [LicenseCategory.B], QuestionSource.School, null,
            [new("Right", true), new("Wrong", false)]).Value)];
    }

    private Contract EnrollStudent()
    {
        var student = User.Create(FullName.Create("Ivanov", "Ivan").Value, Email.Create("ivan@example.com").Value, null, null, _now).Value;
        var contract = Contract.Create("AE-1", student, _course, Money.Create(45000m, _rub).Value, null, _start, _end, _now).Value;
        _group.Enroll(contract, _now);

        return contract;
    }

    private Exam ScheduleTheoryExam() => Exam.Schedule(_group, ExamKind.Theory, _template, _examStart, _now).Value;

    private Exam SchedulePracticeExam() => Exam.Schedule(_group, ExamKind.Practice, null, _examStart, _now).Value;

    private TestSession FinishedSession(Contract contract, Guid? examId, int wrongAnswers)
    {
        var session = TestSession.Start(contract, _template, [_questions], examId, _duringExam).Value;
        foreach (var (question, index) in session.Questions.Select((question, index) => (question, index)).ToList())
        {
            var option = index < wrongAnswers
                ? question.Options.First(candidate => candidate.OptionId != question.CorrectOptionId).OptionId
                : question.CorrectOptionId;
            session.Answer(question.Id, option, _duringExam.AddMinutes(1));
        }

        return session;
    }

    [Fact]
    public void Schedule_TheoryExam_ReturnsScheduledExamAndRaisesScheduledEvent()
    {
        var result = Exam.Schedule(_group, ExamKind.Theory, _template, _examStart, _now);

        result.IsSuccess.Should().BeTrue();
        var exam = result.Value;
        exam.GroupId.Should().Be(_group.Id);
        exam.Kind.Should().Be(ExamKind.Theory);
        exam.TestTemplateId.Should().Be(_template.Id);
        exam.ScheduledAtUtc.Should().Be(_examStart);
        exam.Status.Should().Be(ExamStatus.Scheduled);
        exam.Results.Should().BeEmpty();
        exam.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ExamScheduledDomainEvent(exam.Id, _group.Id, ExamKind.Theory, _examStart, _now));
    }

    [Fact]
    public void Schedule_PracticeExam_ReturnsExamWithoutTemplate()
    {
        var result = Exam.Schedule(_group, ExamKind.Practice, null, _examStart, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.TestTemplateId.Should().BeNull();
    }

    [Fact]
    public void Schedule_InactiveGroup_ReturnsGroupInactiveError()
    {
        _group.Deactivate();

        var result = Exam.Schedule(_group, ExamKind.Practice, null, _examStart, _now);

        result.Error.Should().Be(DomainErrors.Exam.GroupInactive);
    }

    [Fact]
    public void Schedule_UndefinedKind_ReturnsKindNotDefinedError()
    {
        var result = Exam.Schedule(_group, (ExamKind)9, null, _examStart, _now);

        result.Error.Should().Be(DomainErrors.Exam.KindNotDefined);
    }

    [Fact]
    public void Schedule_TheoryWithoutTemplate_ReturnsTemplateRequiredError()
    {
        var result = Exam.Schedule(_group, ExamKind.Theory, null, _examStart, _now);

        result.Error.Should().Be(DomainErrors.Exam.TemplateRequired);
    }

    [Fact]
    public void Schedule_PracticeWithTemplate_ReturnsTemplateNotAllowedError()
    {
        var result = Exam.Schedule(_group, ExamKind.Practice, _template, _examStart, _now);

        result.Error.Should().Be(DomainErrors.Exam.TemplateNotAllowed);
    }

    [Fact]
    public void Schedule_InactiveTemplate_ReturnsTemplateInactiveError()
    {
        _template.Deactivate();

        var result = Exam.Schedule(_group, ExamKind.Theory, _template, _examStart, _now);

        result.Error.Should().Be(DomainErrors.Exam.TemplateInactive);
    }

    [Fact]
    public void Schedule_TemplateOfAnotherCategory_ReturnsTemplateCategoryMismatchError()
    {
        var template = TestTemplate.Create("Final test A", LicenseCategory.A, _template.Rules, [TestBlock.Create(3, []).Value], null).Value;

        var result = Exam.Schedule(_group, ExamKind.Theory, template, _examStart, _now);

        result.Error.Should().Be(DomainErrors.Exam.TemplateCategoryMismatch);
    }

    [Fact]
    public void Schedule_AtCurrentTime_ReturnsScheduledInPastError()
    {
        var result = Exam.Schedule(_group, ExamKind.Practice, null, _now, _now);

        result.Error.Should().Be(DomainErrors.Exam.ScheduledInPast);
    }

    [Fact]
    public void Reschedule_ExamWithoutResults_MovesItAndRaisesRescheduledEvent()
    {
        var exam = SchedulePracticeExam();
        exam.ClearDomainEvents();
        var newStart = _examStart.AddDays(2);

        var result = exam.Reschedule(newStart, _now);

        result.IsSuccess.Should().BeTrue();
        exam.ScheduledAtUtc.Should().Be(newStart);
        exam.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ExamRescheduledDomainEvent(exam.Id, _group.Id, newStart, _now));
    }

    [Fact]
    public void Reschedule_IntoThePast_ReturnsScheduledInPastError()
    {
        var exam = SchedulePracticeExam();

        var result = exam.Reschedule(_now.AddHours(-1), _now);

        result.Error.Should().Be(DomainErrors.Exam.ScheduledInPast);
        exam.ScheduledAtUtc.Should().Be(_examStart);
    }

    [Fact]
    public void Reschedule_ExamWithResults_ReturnsHasResultsError()
    {
        var exam = SchedulePracticeExam();
        exam.RecordPracticeResult(_group, EnrollStudent(), true, 90, null, _instructorId, _duringExam);

        var result = exam.Reschedule(_examStart.AddDays(2), _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.HasResults);
    }

    [Fact]
    public void Cancel_ExamWithoutResults_CancelsAndRaisesCancelledEvent()
    {
        var exam = SchedulePracticeExam();
        exam.ClearDomainEvents();

        var result = exam.Cancel("  Examiner unavailable  ", _now);

        result.IsSuccess.Should().BeTrue();
        exam.Status.Should().Be(ExamStatus.Cancelled);
        exam.CancellationReason.Should().Be("Examiner unavailable");
        exam.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new ExamCancelledDomainEvent(exam.Id, _group.Id, "Examiner unavailable", _now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Cancel_EmptyReason_ReturnsCancellationReasonEmptyError(string? reason)
    {
        var exam = SchedulePracticeExam();

        var result = exam.Cancel(reason, _now);

        result.Error.Should().Be(DomainErrors.Exam.CancellationReasonEmpty);
        exam.Status.Should().Be(ExamStatus.Scheduled);
    }

    [Fact]
    public void Cancel_CancelledExam_ReturnsNotScheduledError()
    {
        var exam = SchedulePracticeExam();
        exam.Cancel("Examiner unavailable", _now);

        var result = exam.Cancel("Again", _now);

        result.Error.Should().Be(DomainErrors.Exam.NotScheduled);
    }

    [Fact]
    public void RecordTheoryResult_PassedSession_RecordsResultFromTheAttempt()
    {
        var contract = EnrollStudent();
        var exam = ScheduleTheoryExam();
        var session = FinishedSession(contract, exam.Id, wrongAnswers: 1);
        exam.ClearDomainEvents();

        var result = exam.RecordTheoryResult(_group, session, _duringExam.AddMinutes(5));

        result.IsSuccess.Should().BeTrue();
        var examResult = result.Value;
        examResult.ContractId.Should().Be(contract.Id);
        examResult.StudentId.Should().Be(contract.StudentId);
        examResult.Passed.Should().BeTrue();
        examResult.Score.Should().Be(2);
        examResult.TestSessionId.Should().Be(session.Id);
        examResult.RecordedByUserId.Should().BeNull();
        examResult.RecordedAtUtc.Should().Be(_duringExam.AddMinutes(5));
        exam.Results.Should().ContainSingle().Which.Should().BeSameAs(examResult);
        exam.DomainEvents.Should().ContainSingle().Which.Should().Be(new ExamResultRecordedDomainEvent(
            exam.Id, contract.Id, contract.StudentId, ExamKind.Theory, true, _duringExam.AddMinutes(5)));
    }

    [Fact]
    public void RecordTheoryResult_FailedSession_RecordsFailure()
    {
        var contract = EnrollStudent();
        var exam = ScheduleTheoryExam();
        var session = FinishedSession(contract, exam.Id, wrongAnswers: 2);

        var result = exam.RecordTheoryResult(_group, session, _duringExam.AddMinutes(5));

        result.Value.Passed.Should().BeFalse();
    }

    [Fact]
    public void RecordTheoryResult_PracticeExam_ReturnsWrongKindError()
    {
        var contract = EnrollStudent();
        var exam = SchedulePracticeExam();

        var result = exam.RecordTheoryResult(_group, FinishedSession(contract, exam.Id, 0), _duringExam.AddMinutes(5));

        result.Error.Should().Be(DomainErrors.Exam.WrongKind);
    }

    [Fact]
    public void RecordTheoryResult_PracticeSession_ReturnsSessionNotForExamError()
    {
        var contract = EnrollStudent();
        var exam = ScheduleTheoryExam();

        var result = exam.RecordTheoryResult(_group, FinishedSession(contract, null, 0), _duringExam.AddMinutes(5));

        result.Error.Should().Be(DomainErrors.Exam.SessionNotForExam);
    }

    [Fact]
    public void RecordTheoryResult_SessionInProgress_ReturnsSessionNotFinishedError()
    {
        var contract = EnrollStudent();
        var exam = ScheduleTheoryExam();
        var session = TestSession.Start(contract, _template, [_questions], exam.Id, _duringExam).Value;

        var result = exam.RecordTheoryResult(_group, session, _duringExam.AddMinutes(1));

        result.Error.Should().Be(DomainErrors.Exam.SessionNotFinished);
    }

    [Fact]
    public void RecordTheoryResult_SecondResultForStudent_ReturnsAlreadyRecordedError()
    {
        var contract = EnrollStudent();
        var exam = ScheduleTheoryExam();
        exam.RecordTheoryResult(_group, FinishedSession(contract, exam.Id, 0), _duringExam.AddMinutes(5));

        var result = exam.RecordTheoryResult(_group, FinishedSession(contract, exam.Id, 0), _duringExam.AddMinutes(10));

        result.Error.Should().Be(DomainErrors.Exam.AlreadyRecorded);
        exam.Results.Should().ContainSingle();
    }

    [Fact]
    public void RecordPracticeResult_PracticeExam_RecordsInstructorResult()
    {
        var contract = EnrollStudent();
        var exam = SchedulePracticeExam();

        var result = exam.RecordPracticeResult(_group, contract, false, 7, "  Failed the hill start  ", _instructorId, _duringExam);

        result.IsSuccess.Should().BeTrue();
        var examResult = result.Value;
        examResult.Passed.Should().BeFalse();
        examResult.Score.Should().Be(7);
        examResult.Comment.Should().Be("Failed the hill start");
        examResult.RecordedByUserId.Should().Be(_instructorId);
        examResult.TestSessionId.Should().BeNull();
    }

    [Fact]
    public void RecordPracticeResult_TheoryExam_ReturnsWrongKindError()
    {
        var exam = ScheduleTheoryExam();

        var result = exam.RecordPracticeResult(_group, EnrollStudent(), true, null, null, _instructorId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.WrongKind);
    }

    [Fact]
    public void RecordPracticeResult_BeforeStart_ReturnsNotStartedError()
    {
        var exam = SchedulePracticeExam();

        var result = exam.RecordPracticeResult(_group, EnrollStudent(), true, null, null, _instructorId, _examStart.AddTicks(-1));

        result.Error.Should().Be(DomainErrors.Exam.NotStarted);
    }

    [Fact]
    public void RecordPracticeResult_CancelledExam_ReturnsNotScheduledError()
    {
        var contract = EnrollStudent();
        var exam = SchedulePracticeExam();
        exam.Cancel("Examiner unavailable", _now);

        var result = exam.RecordPracticeResult(_group, contract, true, null, null, _instructorId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.NotScheduled);
    }

    [Fact]
    public void RecordPracticeResult_OtherGroup_ReturnsGroupMismatchError()
    {
        var contract = EnrollStudent();
        var exam = SchedulePracticeExam();
        var otherGroup = StudyGroup.Create("B-2026-03", _course, _instructorId, null, _start, _end, 20).Value;

        var result = exam.RecordPracticeResult(otherGroup, contract, true, null, null, _instructorId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.GroupMismatch);
    }

    [Fact]
    public void RecordPracticeResult_StudentWhoLeftTheGroup_ReturnsNotGroupMemberError()
    {
        var contract = EnrollStudent();
        _group.Withdraw(contract.Id, _now);
        var exam = SchedulePracticeExam();

        var result = exam.RecordPracticeResult(_group, contract, true, null, null, _instructorId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.NotGroupMember);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void RecordPracticeResult_ScoreOutOfRange_ReturnsScoreOutOfRangeError(int score)
    {
        var exam = SchedulePracticeExam();

        var result = exam.RecordPracticeResult(_group, EnrollStudent(), true, score, null, _instructorId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.ScoreOutOfRange);
        exam.Results.Should().BeEmpty();
    }

    [Fact]
    public void RecordPracticeResult_EmptyInstructorId_ReturnsUserIdEmptyError()
    {
        var exam = SchedulePracticeExam();

        var result = exam.RecordPracticeResult(_group, EnrollStudent(), true, null, null, Guid.Empty, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.UserIdEmpty);
    }

    [Fact]
    public void RecordPracticeResult_CommentTooLong_ReturnsCommentTooLongError()
    {
        var exam = SchedulePracticeExam();

        var result = exam.RecordPracticeResult(_group, EnrollStudent(), true, null, new string('a', 2001), _instructorId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.CommentTooLong);
    }

    [Fact]
    public void CorrectResult_RecordedResult_ChangesItAndRecordsWhoCorrected()
    {
        var contract = EnrollStudent();
        var exam = SchedulePracticeExam();
        exam.RecordPracticeResult(_group, contract, false, 7, null, _instructorId, _duringExam);
        exam.ClearDomainEvents();
        var correctedAt = _duringExam.AddDays(1);

        var result = exam.CorrectResult(contract.Id, true, 2, "Video review", _managerId, correctedAt);

        result.IsSuccess.Should().BeTrue();
        var examResult = exam.Results[0];
        examResult.Passed.Should().BeTrue();
        examResult.Score.Should().Be(2);
        examResult.Comment.Should().Be("Video review");
        examResult.CorrectedByUserId.Should().Be(_managerId);
        examResult.CorrectedAtUtc.Should().Be(correctedAt);
        examResult.RecordedByUserId.Should().Be(_instructorId);
        exam.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ExamResultRecordedDomainEvent>();
    }

    [Fact]
    public void CorrectResult_TheoryResult_CanBeCorrected()
    {
        var contract = EnrollStudent();
        var exam = ScheduleTheoryExam();
        exam.RecordTheoryResult(_group, FinishedSession(contract, exam.Id, 2), _duringExam.AddMinutes(5));

        var result = exam.CorrectResult(contract.Id, true, 3, "Question 2 was wrong in the bank", _managerId, _duringExam.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        exam.Results[0].Passed.Should().BeTrue();
    }

    [Fact]
    public void CorrectResult_NoResult_ReturnsResultNotFoundError()
    {
        var exam = SchedulePracticeExam();

        var result = exam.CorrectResult(Guid.CreateVersion7(), true, null, null, _managerId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.ResultNotFound);
    }

    [Fact]
    public void CorrectResult_ScoreOutOfRange_ReturnsScoreOutOfRangeErrorAndKeepsResult()
    {
        var contract = EnrollStudent();
        var exam = SchedulePracticeExam();
        exam.RecordPracticeResult(_group, contract, false, 7, null, _instructorId, _duringExam);

        var result = exam.CorrectResult(contract.Id, true, -5, null, _managerId, _duringExam);

        result.Error.Should().Be(DomainErrors.Exam.ScoreOutOfRange);
        exam.Results[0].Passed.Should().BeFalse();
    }
}
