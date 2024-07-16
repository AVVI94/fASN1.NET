using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

public class GeneralizedTimeParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        //s = $"{s.Substring(0, 4)}/{s.Substring(4, 2)}/{s.Substring(6, 2)} {s.Substring(8, 2)}:{s.Substring(10, 2)}:{s.Substring(12, 2)} UTC";
        var str = Encoding.ASCII.GetString(content);
        var y = int.Parse(str.Substring(0, 4));
        var m = int.Parse(str.Substring(4, 2));
        var d = int.Parse(str.Substring(6, 2));
        var h = int.Parse(str.Substring(8, 2));
        var min = int.Parse(str.Substring(10, 2));
        var sec = int.Parse(str.Substring(12, 2));
        var ms = str.Substring(14);
        var utc = false;
        if (ms.Contains(".") || ms.Contains("-") || ms.Contains("Z"))
        {
            //if (ms.IndexOf(".") != -1)
            //{
            //    ms = ms.Substring(0, ms.IndexOf("."));
            //}
            //if (ms.IndexOf("-") != -1)
            //{
            //    ms = ms.Substring(0, ms.IndexOf("-"));
            //}
            if (ms.IndexOf("Z") != -1)
            {
                //ms = ms.Substring(0, ms.IndexOf("Z"));
                utc = true;
            }
        }
        return new DateTime(y, m, d, h, min, sec, utc ? DateTimeKind.Utc : DateTimeKind.Local).ToString();
    }

    public static GeneralizedTimeParsingStrategy Default { get; } = new GeneralizedTimeParsingStrategy();
}
