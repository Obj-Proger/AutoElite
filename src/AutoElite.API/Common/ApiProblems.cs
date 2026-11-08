using System.Globalization;
using System.Text;
using System.Text.Json;
using AutoElite.Application.Common;
using AutoElite.Application.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace AutoElite.API.Common;

/// <summary>A field that failed validation, as an error response lists it.</summary>
/// <param name="Field">The field as the request names it in JSON, such as <c>items[0].price</c>.</param>
/// <param name="Code">The code of the failure, by which clients choose their own text.</param>
/// <param name="Message">The text of the failure in the language of the request.</param>
public sealed record ApiFieldError(string Field, string Code, string Message);

/// <summary>
/// Writes the errors of the application as problem details (RFC 9457), the one shape of every error response: the
/// status, a title and the message of the error in the language of the request in <c>detail</c>, its <c>code</c>, the
/// failed fields in <c>errors</c>, and the <c>traceId</c> that finds the request in the logs. Field names are written
/// as the JSON of the request writes them, in camel case, so that a client finds the field of its form.
/// </summary>
internal static class ApiProblems
{
    /// <summary>The extension with the code of the error.</summary>
    public const string CodeExtension = "code";

    /// <summary>The extension with the failed fields.</summary>
    public const string ErrorsExtension = "errors";

    /// <summary>Gets the status code of an error type.</summary>
    /// <param name="type">The type of the error.</param>
    public static int StatusCodeOf(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

    /// <summary>Creates the problem details of an error, translated into the language of the request.</summary>
    /// <param name="httpContext">The request.</param>
    /// <param name="error">The error.</param>
    public static ProblemDetails Create(HttpContext httpContext, Error error)
    {
        var services = httpContext.RequestServices;
        var localized = services.GetRequiredService<IErrorLocalizer>().Localize(error, CultureInfo.CurrentUICulture);
        var problem = services.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(httpContext, StatusCodeOf(localized.Type), detail: localized.Message);

        problem.Extensions[CodeExtension] = localized.Code;
        if (localized.FieldErrors is { Count: > 0 } fieldErrors)
        {
            problem.Extensions[ErrorsExtension] = fieldErrors
                .Select(fieldError => new ApiFieldError(JsonFieldName(fieldError.Field), fieldError.Code, fieldError.Message))
                .ToList();
        }

        return problem;
    }

    /// <summary>
    /// Creates the response to a request whose body or parameters could not be read, such as malformed JSON: a
    /// validation error with every unreadable field, in the shape of the other errors.
    /// </summary>
    /// <param name="context">The action whose arguments could not be bound.</param>
    public static IActionResult FromModelState(ActionContext context)
    {
        var unreadable = ApplicationErrors.Request.UnreadableValue;
        var fieldErrors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => FieldOfModelKey(entry.Key))
            .Where(field => field.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Select(field => new FieldError(field, unreadable.Code, unreadable.Message))
            .ToList();
        var problem = Create(context.HttpContext, ApplicationErrors.ValidationFailed(fieldErrors));

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    /// <summary>
    /// Writes a field name of the application, which follows the C# names (<c>Items[0].Price</c>), as the JSON of the
    /// request writes it (<c>items[0].price</c>). What is in brackets, an index or a key such as a file name, is kept.
    /// </summary>
    /// <param name="field">The field name.</param>
    public static string JsonFieldName(string field)
    {
        var name = new StringBuilder(field.Length);
        var depth = 0;
        var segmentStart = -1;
        for (var index = 0; index < field.Length; index++)
        {
            var character = field[index];
            if (depth == 0 && character is not ('.' or '['))
            {
                if (segmentStart < 0)
                    segmentStart = index;

                continue;
            }

            WriteSegment(index);
            name.Append(character);
            if (character == '[')
                depth++;
            else if (character == ']' && depth > 0)
                depth--;
        }

        WriteSegment(field.Length);

        return name.ToString();

        void WriteSegment(int end)
        {
            if (segmentStart < 0)
                return;

            name.Append(JsonNamingPolicy.CamelCase.ConvertName(field[segmentStart..end]));
            segmentStart = -1;
        }
    }

    /// <summary>
    /// Gets the field of a model state key: the JSON path of the body without its root (<c>$.items[0].price</c>), or
    /// the name of a route or query parameter.
    /// </summary>
    private static string FieldOfModelKey(string key) =>
        key.StartsWith("$.", StringComparison.Ordinal) ? key[2..]
        : key == "$" ? string.Empty
        : JsonFieldName(key);
}
