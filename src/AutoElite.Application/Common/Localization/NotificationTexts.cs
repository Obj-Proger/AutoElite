using System.Globalization;
using System.Resources;

namespace AutoElite.Application.Common.Localization;

/// <summary>
/// The texts of in-app notifications, from the <c>NotificationTexts</c> resources: English by default and a
/// translation for every other supported language. Each notification has a <c>{Name}.Title</c> and a
/// <c>{Name}.Body</c> text; the constants are the names.
/// </summary>
/// <remarks>
/// Bodies format dates in the recipient's language, so each translation chooses its own date format. Values that
/// need translating themselves, such as the kind of an exam, are passed as a <see cref="NotificationTerm"/> and
/// are written in the recipient's language too, as <c>{Type}.{Member}</c> texts.
/// </remarks>
internal static class NotificationTexts
{
    /// <summary>
    /// A lead without a manager arrived: <c>{0}</c> is the contact name, <c>{1}</c> the source, a
    /// <see cref="Term(LeadSource)"/>.
    /// </summary>
    public const string NewLead = "NewLead";

    /// <summary>A lead was assigned to the recipient: <c>{0}</c> is the contact name.</summary>
    public const string LeadAssigned = "LeadAssigned";

    /// <summary>
    /// A student booked a driving lesson of the instructor: <c>{0}</c> is the student, <c>{1}</c> the local start.
    /// </summary>
    public const string DrivingLessonBooked = "DrivingLessonBooked";

    /// <summary>A driving booking was cancelled: <c>{0}</c> is the local start, <c>{1}</c> the reason.</summary>
    public const string DrivingBookingCancelled = "DrivingBookingCancelled";

    /// <summary>The student is invited to rate a completed driving lesson: <c>{0}</c> is the local start.</summary>
    public const string RateDrivingLesson = "RateDrivingLesson";

    /// <summary>
    /// A driving lesson got a low rating: <c>{0}</c> is the student, <c>{1}</c> the instructor, <c>{2}</c> the local
    /// start, <c>{3}</c> the rating.
    /// </summary>
    public const string LowDrivingLessonRating = "LowDrivingLessonRating";

    /// <summary>
    /// A theory lesson of the group was cancelled: <c>{0}</c> is the group, <c>{1}</c> the local start, <c>{2}</c> the
    /// reason.
    /// </summary>
    public const string TheoryLessonCancelled = "TheoryLessonCancelled";

    /// <summary>A theory lesson of the group was moved: <c>{0}</c> is the group, <c>{1}</c> the new local start.</summary>
    public const string TheoryLessonRescheduled = "TheoryLessonRescheduled";

    /// <summary>
    /// An exam of the group was scheduled: <c>{0}</c> is the kind, a <see cref="Term(ExamKind)"/>, <c>{1}</c> the
    /// group, <c>{2}</c> the local time.
    /// </summary>
    public const string ExamScheduled = "ExamScheduled";

    /// <summary>
    /// An exam of the group was moved: <c>{0}</c> is the kind, a <see cref="Term(ExamKind)"/>, <c>{1}</c> the group,
    /// <c>{2}</c> the new local time.
    /// </summary>
    public const string ExamRescheduled = "ExamRescheduled";

    /// <summary>
    /// An exam of the group was cancelled: <c>{0}</c> is the kind, a <see cref="Term(ExamKind)"/>, <c>{1}</c> the
    /// group, <c>{2}</c> the local time, <c>{3}</c> the reason.
    /// </summary>
    public const string ExamCancelled = "ExamCancelled";

    /// <summary>The student passed an exam: <c>{0}</c> is the kind, a <see cref="Term(ExamKind)"/>.</summary>
    public const string ExamPassed = "ExamPassed";

    /// <summary>The student did not pass an exam: <c>{0}</c> is the kind, a <see cref="Term(ExamKind)"/>.</summary>
    public const string ExamFailed = "ExamFailed";

    /// <summary>The student joined a study group: <c>{0}</c> is the group, <c>{1}</c> the start date.</summary>
    public const string EnrolledInGroup = "EnrolledInGroup";

    /// <summary>
    /// A payment of the student is confirmed: <c>{0}</c> is the amount, <c>{1}</c> the currency, <c>{2}</c> the
    /// contract number.
    /// </summary>
    public const string PaymentConfirmed = "PaymentConfirmed";

    /// <summary>
    /// A payment of the student is refunded: <c>{0}</c> is the amount, <c>{1}</c> the currency, <c>{2}</c> the contract
    /// number.
    /// </summary>
    public const string PaymentRefunded = "PaymentRefunded";

    /// <summary>Gets the notification text resources.</summary>
    internal static ResourceManager Resources { get; } =
        new("AutoElite.Application.Common.Localization.NotificationTexts", typeof(NotificationTexts).Assembly);

    /// <summary>Gets the title of a notification in the culture. Titles have no placeholders.</summary>
    /// <param name="name">The name of the notification, one of the constants of this class.</param>
    /// <param name="culture">The culture of the recipient.</param>
    public static string Title(string name, CultureInfo culture) => Get($"{name}.Title", culture);

    /// <summary>Gets the body of a notification in the culture and fills in its placeholders.</summary>
    /// <param name="name">The name of the notification, one of the constants of this class.</param>
    /// <param name="culture">The culture of the recipient.</param>
    /// <param name="arguments">The values of the placeholders, in order.</param>
    public static string Body(string name, CultureInfo culture, params object?[] arguments) =>
        string.Format(
            culture,
            Get($"{name}.Body", culture),
            [.. arguments.Select(argument => argument is NotificationTerm term ? Get(term.Key, culture) : argument)]);

    /// <summary>Gets a body value that is written in the recipient's language.</summary>
    /// <param name="source">The source of a lead.</param>
    public static NotificationTerm Term(LeadSource source) => new($"{nameof(LeadSource)}.{source}");

    /// <summary>Gets a body value that is written in the recipient's language.</summary>
    /// <param name="kind">The kind of an exam.</param>
    public static NotificationTerm Term(ExamKind kind) => new($"{nameof(ExamKind)}.{kind}");

    private static string Get(string key, CultureInfo culture) =>
        Resources.GetString(key, culture) ?? throw new InvalidOperationException($"The notification text '{key}' is missing.");
}
