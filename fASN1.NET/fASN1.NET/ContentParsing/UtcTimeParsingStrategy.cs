using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

public class UtcTimeParsingStrategy : IContentParsingStrategy
{
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

    public static UtcTimeParsingStrategy Default { get; } = new UtcTimeParsingStrategy();
}