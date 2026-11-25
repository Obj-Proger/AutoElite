namespace AutoElite.Application.Settings;

/// <summary>Where the website of the school is, for the links of the mobile application to it.</summary>
/// <param name="Address">The absolute address of the start page of the website, with a trailing slash.</param>
public sealed record WebsiteDetails(string Address);
