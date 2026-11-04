using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Users;

/// <summary>Writes the emails the system sends to a user, in the user's language.</summary>
internal static class UserEmail
{
    /// <summary>
    /// Writes an email from the texts in <see cref="EmailTexts"/>. The first placeholder of every body is the
    /// user's first name; the arguments fill the placeholders after it.
    /// </summary>
    /// <param name="user">The user the email is about, whose language and name are used.</param>
    /// <param name="to">The address to send to, usually the user's own.</param>
    /// <param name="subjectKey">The key of the subject text.</param>
    /// <param name="bodyKey">The key of the body text.</param>
    /// <param name="arguments">The values of the body placeholders after the first name, in order.</param>
    public static EmailMessage Compose(User user, Email to, string subjectKey, string bodyKey, params object?[] arguments)
    {
        var culture = user.PreferredLanguage.ToCulture();

        return new EmailMessage(
            to,
            EmailTexts.Format(subjectKey, culture),
            EmailTexts.Format(bodyKey, culture, [user.FullName.FirstName, .. arguments]));
    }
}
