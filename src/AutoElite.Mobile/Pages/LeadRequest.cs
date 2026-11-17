namespace AutoElite.Mobile.Pages;

/// <summary>The lead a manager opens, passed to the page of the lead.</summary>
/// <param name="LeadId">The lead.</param>
/// <param name="Name">The name the person gave, shown while the lead is read.</param>
public sealed record LeadRequest(Guid LeadId, string Name);
