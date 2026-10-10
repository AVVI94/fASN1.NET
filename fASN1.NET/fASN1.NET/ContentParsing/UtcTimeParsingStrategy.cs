using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows UTCTime content as a UTC <see cref="System.DateTime"/> formatted for the current culture. Two-digit years below 70 are taken as 20xx, the others as 19xx.
/// </summary>
public class UtcTimeParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        var str = Encoding.ASCII.GetString(content);
        if (int.Parse(str.Substring(0, 2)) < 70)
        {
            str = "20" + str;
        }
        else
        {
            str = "19" + str;
        }
        var y = int.Parse(str.Substring(0, 4));
        var m = int.Parse(str.Substring(4, 2));
        var d = int.Parse(str.Substring(6, 2));
        var h = int.Parse(str.Substring(8, 2));
        var min = int.Parse(str.Substring(10, 2));
        var sec = int.Parse(str.Substring(12, 2));
        var ms = str.Substring(14);
        return new DateTime(y, m, d, h, min, sec, DateTimeKind.Utc).ToString();
    }
    /// <summary>
    /// Formats a date the way the <see cref="Tags.UTCTime"/> constructor does: <c>yyMMddHHmmss</c>, with the first two
    /// digits then replaced by 19 or 20.
    /// </summary>
    /// <param name="dateTime">The date and time.</param>
    /// <returns>The text.</returns>
    public string Reverse(DateTime dateTime)
    {
        var str = dateTime.ToString("yyMMddHHmmss");
        if (int.Parse(str.Substring(0, 2)) >= 70)
        {
            str = "19" + str.Substring(2);
        }
        else
        {
            str = "20" + str.Substring(2);
        }
        return str;
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static UtcTimeParsingStrategy Default { get; } = new UtcTimeParsingStrategy();
}