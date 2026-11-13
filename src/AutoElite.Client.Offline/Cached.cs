namespace AutoElite.Client.Offline;

/// <summary>What the API answered, either now or, when it could not be reached, the last time.</summary>
/// <typeparam name="T">The type of the answer.</typeparam>
/// <param name="Value">The answer.</param>
/// <param name="SavedAt">When the API gave the answer.</param>
/// <param name="IsFromCache">
/// <see langword="true"/> when the API could not be reached and the answer is the one kept on the device, which the
/// application shows as possibly out of date.
/// </param>
public sealed record Cached<T>(T Value, DateTimeOffset SavedAt, bool IsFromCache);
