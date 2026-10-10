using System;
using System.Text;
using fASN1.NET.Tags;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows UTCTime content as a UTC <see cref="DateTime"/> formatted for the current culture. Following RFC 5280,
/// two-digit years 50–99 are taken as 19xx and 00–49 as 20xx.
/// </summary>
public class UtcTimeParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        var str = Encoding.ASCII.GetString(content);
        var year = UTCTime.ExpandYear(int.Parse(str.Substring(0, 2)));
        var month = int.Parse(str.Substring(2, 2));
        var day = int.Parse(str.Substring(4, 2));
        var hour = int.Parse(str.Substring(6, 2));
        var minute = int.Parse(str.Substring(8, 2));
        var second = int.Parse(str.Substring(10, 2));
        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).ToString();
    }

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
