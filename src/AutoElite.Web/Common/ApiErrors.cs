using System.Text.Json;
using AutoElite.Client.Api.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace AutoElite.Web.Common;

/// <summary>
/// The messages of an error of the API, which the API writes in the language of the browser, as its Accept-Language
/// header asks: the message of each field that failed validation, or else the detail of the problem.
/// </summary>
internal static class ApiErrors
{
    /// <summary>Gives the messages of an error.</summary>
    /// <param name="exception">The error.</param>
    /// <returns>The messages, empty when the API gave none.</returns>
    public static IReadOnlyList<string> MessagesOf(ApiException exception)
    {
        if (exception is not ProblemDetails problem)
            return [];

        List<string> messages = [.. FieldMessagesOf(problem)];
        if (messages.Count == 0 && problem.Detail is { Length: > 0 } detail)
            messages.Add(detail);

        return messages;
    }

    // The failed fields are in the "errors" extension of the problem, each with its message.
    private static IEnumerable<string> FieldMessagesOf(ProblemDetails problem)
    {
        if (!problem.AdditionalData.TryGetValue("errors", out var errors))
            yield break;

        if (errors is UntypedArray array)
        {
            foreach (var item in array.GetValue())
            {
                if (item is UntypedObject field && field.GetValue().TryGetValue("message", out var value)
                    && value is UntypedString message && message.GetValue() is { Length: > 0 } text)
                {
                    yield return text;
                }
            }
        }
        else if (errors is JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String && message.GetString() is { Length: > 0 } text)
                {
                    yield return text;
                }
            }
        }
    }
}
