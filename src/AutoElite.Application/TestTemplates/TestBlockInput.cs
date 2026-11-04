namespace AutoElite.Application.TestTemplates;

/// <summary>A block of a test: how many questions to draw and from which topics.</summary>
/// <param name="QuestionCount">The number of questions drawn for the block.</param>
/// <param name="TopicIds">The course topics to draw from; empty for all topics, which only a single block can do.</param>
public sealed record TestBlockInput(int QuestionCount, IReadOnlyList<Guid>? TopicIds);
