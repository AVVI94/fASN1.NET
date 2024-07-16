using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

public class OctetStringParsingStrategy : IContentParsingStrategy
{
    private const int SPACE_CHAR_CODE = 32;
    private const int TAB_CHAR_CODE = 9;
    private const int LF_CHARD_CODE = 10;
    private const int CR_CHAR_CODE = 13;

    public string Parse(byte[] content)
    {
        var sb = new StringBuilder();
        var s = Encoding.UTF8.GetString(content);
        for (var i = 0; i < s.Length; ++i)
        {
            var c = s[i];
            if ((int)c
                is < SPACE_CHAR_CODE
                and not TAB_CHAR_CODE
                and not LF_CHARD_CODE
                and not CR_CHAR_CODE)
            {
                //throw new Exception("Unprintable character");
                return BitConverter.ToString(content).Replace("-", "");
            }
            _ = sb.Append(c);
        }
        return sb.ToString();
    }

    public static OctetStringParsingStrategy Default { get; } = new OctetStringParsingStrategy();
}
