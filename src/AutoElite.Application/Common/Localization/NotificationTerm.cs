namespace AutoElite.Application.Common.Localization;

/// <summary>A notification body value that is translated into the recipient's language when the body is written.</summary>
/// <param name="Key">The key of the text, <c>{Type}.{Member}</c>.</param>
internal sealed record NotificationTerm(string Key);
