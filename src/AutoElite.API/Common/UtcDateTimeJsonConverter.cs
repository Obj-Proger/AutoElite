using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoElite.API.Common;

/// <summary>
/// Reads a time of a request with an offset, such as <c>2026-11-16T09:00:00+05:00</c> or the <c>+00:00</c> that the
/// generated client writes, as the same moment in UTC rather than in the local time of the server, which the rules of
/// the application reject; a time with <c>Z</c> is UTC already, and one without an offset stays unspecified, so that
/// those rules still reject it. Times are written as they are, which is with <c>Z</c> for times in UTC.
/// </summary>
internal sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // The reader converts a time with an offset to the local time of the server; the offset itself gives the moment
        // without the rules of that time zone, which are ambiguous when its clocks go back.
        var time = reader.GetDateTime();

        return time.Kind == DateTimeKind.Local ? reader.GetDateTimeOffset().UtcDateTime : time;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value);
    }
}
