using System.Globalization;

namespace AutoElite.Application.Common.Localization;

/// <summary>
/// Translates the messages of an error into the user's language by error code. The API calls it before writing
/// an error response; codes stay unchanged, so clients can still rely on them.
/// </summary>
public interface IErrorLocalizer
{
    /// <summary>
    /// Returns a copy of the error whose message and field error messages are in the given culture.
    /// A message without a translation keeps its original text.
    /// </summary>
    /// <param name="error">The error to translate.</param>
    /// <param name="culture">The culture to translate into, for example the user's preferred language.</param>
    Error Localize(Error error, CultureInfo culture);
}
