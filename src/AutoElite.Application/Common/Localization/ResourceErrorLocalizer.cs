using System.Globalization;
using System.Resources;

namespace AutoElite.Application.Common.Localization;

/// <summary>
/// Default <see cref="IErrorLocalizer"/>, backed by the <c>ErrorMessages</c> resources: Russian texts for every code
/// and English texts only where the message in code is not good enough for users.
/// </summary>
internal sealed class ResourceErrorLocalizer : IErrorLocalizer
{
    /// <summary>Gets the error message resources, keyed by error code.</summary>
    internal static ResourceManager Resources { get; } =
        new("AutoElite.Application.Common.Localization.ErrorMessages", typeof(ResourceErrorLocalizer).Assembly);

    /// <inheritdoc />
    public Error Localize(Error error, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(culture);

        var fieldErrors = error.FieldErrors?
            .Select(fieldError => fieldError with { Message = Translate(fieldError.Code, culture) ?? fieldError.Message })
            .ToList();

        return error with { Message = Translate(error.Code, culture) ?? error.Message, FieldErrors = fieldErrors };
    }

    private static string? Translate(string code, CultureInfo culture) =>
        string.IsNullOrEmpty(code) ? null : Resources.GetString(code, culture);
}
