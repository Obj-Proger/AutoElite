using AutoElite.Application.Practice;

namespace AutoElite.Application.Tests.Practice;

public class PracticeOrderTests
{
    private static readonly IReadOnlyList<Guid> _questions = [.. Enumerable.Range(1, 40).Select(_ => Guid.NewGuid()).Order()];

    [Fact]
    public void Shuffle_TheSameSeed_GivesTheSameOrder()
    {
        PracticeOrder.Shuffle(_questions, 42).Should().Equal(PracticeOrder.Shuffle(_questions, 42));
    }

    [Fact]
    public void Shuffle_KeepsEveryQuestionOnce()
    {
        PracticeOrder.Shuffle(_questions, 7).Should().BeEquivalentTo(_questions).And.OnlyHaveUniqueItems();
    }

    [Fact]
    public void Shuffle_AnotherSeed_GivesAnotherOrder()
    {
        PracticeOrder.Shuffle(_questions, 1).Should().NotEqual(PracticeOrder.Shuffle(_questions, 2));
    }

    [Fact]
    public void Shuffle_NoQuestions_GivesNone()
    {
        PracticeOrder.Shuffle([], 3).Should().BeEmpty();
    }
}
