namespace AutoElite.Application.Common.Abstractions;

/// <summary>A plain-text email to one recipient, already written in the recipient's language.</summary>
/// <param name="To">The recipient's address.</param>
/// <param name="Subject">The subject line.</param>
/// <param name="Body">The plain-text body.</param>
public sealed record EmailMessage(Email To, string Subject, string Body);
