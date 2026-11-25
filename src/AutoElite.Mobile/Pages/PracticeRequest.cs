using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Pages;

/// <summary>A practice to take, passed from the hub of the practice to its page.</summary>
/// <param name="ContractId">The contract of the student, whose licence category chooses the questions.</param>
/// <param name="Mode">Which questions are practised.</param>
/// <param name="TopicId">The topic, for the practice of a topic.</param>
/// <param name="Title">The name of the practice, shown as the title of its page.</param>
public sealed record PracticeRequest(Guid ContractId, PracticeMode Mode, Guid? TopicId, string Title);
