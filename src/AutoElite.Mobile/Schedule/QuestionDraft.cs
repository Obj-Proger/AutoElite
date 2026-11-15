using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Schedule;

/// <summary>The content of a question that the instructor writes.</summary>
/// <param name="Text">The question.</param>
/// <param name="Explanation">The explanation shown after the test, or <see langword="null"/>.</param>
/// <param name="TopicId">The topic of a course.</param>
/// <param name="Categories">The licence categories the question applies to.</param>
/// <param name="ExternalCode">The code in the official set, kept as it was, or <see langword="null"/>.</param>
/// <param name="Options">The options, in their order, one of them correct.</param>
public sealed record QuestionDraft(
    string Text,
    string? Explanation,
    Guid TopicId,
    IReadOnlyList<LicenseCategory> Categories,
    string? ExternalCode,
    IReadOnlyList<(string Text, bool IsCorrect)> Options);
