namespace AutoElite.Domain.Tests.Entities;

public class PracticeRecordTests
{
    private static readonly DateTime _now = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid _studentId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _questionId = new("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Start_RecordsTheFirstAnswer(bool isCorrect)
    {
        var record = PracticeRecord.Start(_studentId, _questionId, isCorrect, _now).Value;

        record.Id.Should().NotBeEmpty();
        record.StudentId.Should().Be(_studentId);
        record.QuestionId.Should().Be(_questionId);
        record.AnswerCount.Should().Be(1);
        record.MistakeCount.Should().Be(isCorrect ? 0 : 1);
        record.IsLastAnswerCorrect.Should().Be(isCorrect);
        record.IsMistake.Should().Be(!isCorrect);
        record.LastAnsweredAtUtc.Should().Be(_now);
    }

    [Fact]
    public void Start_EmptyStudent_ReturnsStudentIdEmptyError()
    {
        var result = PracticeRecord.Start(Guid.Empty, _questionId, true, _now);

        result.Error.Should().Be(DomainErrors.PracticeRecord.StudentIdEmpty);
    }

    [Fact]
    public void Start_EmptyQuestion_ReturnsQuestionIdEmptyError()
    {
        var result = PracticeRecord.Start(_studentId, Guid.Empty, true, _now);

        result.Error.Should().Be(DomainErrors.PracticeRecord.QuestionIdEmpty);
    }

    [Fact]
    public void Record_AWrongAnswer_MakesTheQuestionAMistake()
    {
        var record = PracticeRecord.Start(_studentId, _questionId, true, _now).Value;

        record.Record(false, _now.AddMinutes(5));

        record.AnswerCount.Should().Be(2);
        record.MistakeCount.Should().Be(1);
        record.IsMistake.Should().BeTrue();
        record.LastAnsweredAtUtc.Should().Be(_now.AddMinutes(5));
    }

    [Fact]
    public void Record_ARightAnswerAfterAMistake_TakesTheQuestionOffTheMistakes()
    {
        var record = PracticeRecord.Start(_studentId, _questionId, false, _now).Value;

        record.Record(true, _now.AddDays(1));

        record.AnswerCount.Should().Be(2);
        record.MistakeCount.Should().Be(1);
        record.IsMistake.Should().BeFalse();
    }
}
