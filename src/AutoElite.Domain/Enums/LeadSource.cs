namespace AutoElite.Domain.Enums;

/// <summary>The channel through which a lead reached the school; used for conversion statistics by source.</summary>
public enum LeadSource
{
    /// <summary>The public application form on the school's website.</summary>
    Website = 1,

    /// <summary>An incoming phone call.</summary>
    PhoneCall = 2,

    /// <summary>A visit to the school's office.</summary>
    WalkIn = 3,

    /// <summary>A recommendation from a current or former student.</summary>
    Referral = 4,

    /// <summary>Social networks and messengers.</summary>
    SocialMedia = 5,

    /// <summary>Paid advertising.</summary>
    Advertising = 6,

    /// <summary>Any other channel; details go to the comment.</summary>
    Other = 7
}
