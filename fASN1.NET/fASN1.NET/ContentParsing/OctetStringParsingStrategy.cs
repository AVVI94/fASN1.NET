using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows content as UTF-8 text when it has no control characters other than tab, line feed and carriage return; otherwise as uppercase hexadecimal. Also used for every non-universal tag.
/// </summary>
public class OctetStringParsingStrategy : IContentParsingStrategy
{
    private const int SPACE_CHAR_CODE = 32;
    private const int TAB_CHAR_CODE = 9;
    private const int LF_CHAR_CODE = 10;
    private const int CR_CHAR_CODE = 13;

    /// <inheritdoc/>

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
                and not LF_CHAR_CODE
                and not CR_CHAR_CODE)
            {
                //throw new Exception("Unprintable character");
                return BitConverter.ToString(content).Replace("-", "");
            }
            _ = sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static OctetStringParsingStrategy Default { get; } = new OctetStringParsingStrategy();
}
