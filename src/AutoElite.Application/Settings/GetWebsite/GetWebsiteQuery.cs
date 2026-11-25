namespace AutoElite.Application.Settings.GetWebsite;

/// <summary>
/// Reads where the website of the school is. Any signed-in user may read it, so that the mobile application can open on
/// the website what it leaves to it, such as drawing up a contract.
/// </summary>
public sealed record GetWebsiteQuery : IQuery<WebsiteDetails>;
