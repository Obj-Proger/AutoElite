namespace AutoElite.Domain.Tests.ValueObjects;

public class TestBlockTests
{
    private static readonly Guid _signsTopicId = new("77777777-7777-7777-7777-777777777777");
    private static readonly Guid _markingsTopicId = new("88888888-8888-8888-8888-888888888888");

    [Fact]
    public void Create_WithTopics_ReturnsBlockLimitedToThem()
    {
        var result = TestBlock.Create(5, [_signsTopicId, _markingsTopicId]);

        result.IsSuccess.Should().BeTrue();
        result.Value.QuestionCount.Should().Be(5);
        result.Value.TopicIds.Should().Equal(_signsTopicId, _markingsTopicId);
        result.Value.CoversAllTopics.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutTopics_ReturnsBlockCoveringAllTopics()
    {
        var result = TestBlock.Create(20, []);

        result.Value.CoversAllTopics.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_QuestionCountOutOfRange_ReturnsQuestionCountOutOfRangeError(int questionCount)
    {
        var result = TestBlock.Create(questionCount, []);

        result.Error.Should().Be(DomainErrors.TestBlock.QuestionCountOutOfRange);
    }

    [Fact]
    public void Create_EmptyTopicId_ReturnsTopicIdEmptyError()
    {
        var result = TestBlock.Create(5, [Guid.Empty]);

        result.Error.Should().Be(DomainErrors.TestBlock.TopicIdEmpty);
    }

    [Fact]
    public void Create_RepeatedTopic_ReturnsDuplicateTopicError()
    {
        var result = TestBlock.Create(5, [_signsTopicId, _signsTopicId]);

        result.Error.Should().Be(DomainErrors.TestBlock.DuplicateTopic);
    }

    [Fact]
    public void Equals_SameCountAndTopics_AreEqual()
    {
        TestBlock.Create(5, [_signsTopicId]).Value.Should().Be(TestBlock.Create(5, [_signsTopicId]).Value);
    }

    [Fact]
    public void Equals_DifferentTopics_AreNotEqual()
    {
        TestBlock.Create(5, [_signsTopicId]).Value.Should().NotBe(TestBlock.Create(5, [_signsTopicId, _markingsTopicId]).Value);
    }
}
