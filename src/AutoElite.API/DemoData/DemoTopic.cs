using AutoElite.Application.Questions;

namespace AutoElite.API.DemoData;

/// <summary>A topic of the demo course with its questions.</summary>
/// <param name="Title">The title of the topic.</param>
/// <param name="Questions">The questions.</param>
internal sealed record DemoTopic(string Title, IReadOnlyList<DemoQuestion> Questions);

/// <summary>A question of the demo practice tests.</summary>
/// <param name="Text">The question.</param>
/// <param name="Explanation">The explanation shown after the test.</param>
/// <param name="Options">The answer options, one of them correct.</param>
internal sealed record DemoQuestion(string Text, string Explanation, IReadOnlyList<AnswerOptionInput> Options);
