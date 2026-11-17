using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>The words of a lead in the language of the user: where it came from and where it is in the funnel.</summary>
internal static class LeadTexts
{
    /// <summary>The channel a lead came through.</summary>
    /// <param name="source">The channel.</param>
    /// <returns>Its name, or <see langword="null"/> when it is not known.</returns>
    public static string? SourceOf(LeadSource? source) => source switch
    {
        LeadSource.Website => AppTexts.Get("Lead.Website"),
        LeadSource.PhoneCall => AppTexts.Get("Lead.PhoneCall"),
        LeadSource.WalkIn => AppTexts.Get("Lead.WalkIn"),
        LeadSource.Referral => AppTexts.Get("Lead.Referral"),
        LeadSource.SocialMedia => AppTexts.Get("Lead.SocialMedia"),
        LeadSource.Advertising => AppTexts.Get("Lead.Advertising"),
        _ => null
    };

    /// <summary>The status of a lead in the funnel.</summary>
    /// <param name="status">The status.</param>
    /// <returns>Its name.</returns>
    public static string StatusOf(LeadStatus? status) => status switch
    {
        LeadStatus.InProgress => AppTexts.Get("Lead.InProgress"),
        LeadStatus.Converted => AppTexts.Get("Lead.Converted"),
        LeadStatus.Lost => AppTexts.Get("Lead.Lost"),
        _ => AppTexts.Get("Lead.New")
    };

    /// <summary>The source, the licence category wanted and the arrival of a lead, as a line.</summary>
    /// <param name="source">The channel.</param>
    /// <param name="category">The licence category wanted, if known.</param>
    /// <param name="createdAtUtc">When the lead arrived.</param>
    /// <returns>The line.</returns>
    public static string DetailsOf(LeadSource? source, LicenseCategory? category, DateTimeOffset? createdAtUtc) =>
        LessonTexts.DetailsOf(
            SourceOf(source),
            category is { } wanted ? AppTexts.Format("Lead.Category", CategoryNames.Of(wanted)) : null,
            createdAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
}
