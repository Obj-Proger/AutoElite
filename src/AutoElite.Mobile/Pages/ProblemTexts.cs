using AutoElite.Client;
using AutoElite.Mobile.Localization;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>What a failed request tells the user: the messages of the API, or that the server cannot be reached.</summary>
internal static class ProblemTexts
{
    /// <summary>Gives the message of a failure.</summary>
    /// <param name="failure">The failure of a request to the API.</param>
    /// <returns>The message, in the language of the application.</returns>
    public static string Of(Exception failure) => failure switch
    {
        ApiException problem when ApiProblems.MessagesOf(problem) is { Count: > 0 } messages => string.Join(Environment.NewLine, messages),
        HttpRequestException or TaskCanceledException => AppTexts.Get("Problems.Unreachable"),
        _ => AppTexts.Get("Problems.Failed")
    };
}
