using System;
using System.Text;
using fASN1.NET.Tags;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows UTCTime content as ISO 8601 text in UTC that does not depend on the current culture, such as
/// <c>2025-01-02T03:04:05Z</c>; a time with an offset is converted to UTC. Following RFC 5280, two-digit years
/// 50–99 are taken as 19xx and 00–49 as 20xx. Content that is not a valid UTCTime is shown as
/// <see cref="OctetStringParsingStrategy"/> would show it.
/// </summary>
public class UtcTimeParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
        => Asn1Time.TryParseUtcTime(content, out var value)
            ? Asn1Time.ToDisplayString(value)
            : OctetStringParsingStrategy.Default.Parse(content);

    /// <summary>
    /// Formats a date as UTCTime text, <c>YYMMDDHHMMSSZ</c> in UTC, as the <see cref="UTCTime(DateTime)"/>
    /// constructor does.
    /// </summary>
    /// <param name="dateTime">The date and time; a time that is not UTC is converted to UTC.</param>
    /// <returns>The text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The year in UTC is outside 1950–2049.</exception>
    public string Reverse(DateTime dateTime) => UTCTime.Format(dateTime);

    /// <summary>A shared instance; the strategy keeps no state.</summary>
    public static UtcTimeParsingStrategy Default { get; } = new UtcTimeParsingStrategy();
}
