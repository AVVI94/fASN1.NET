using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.ContentParsing;
public class Utf8StringParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return Encoding.UTF8.GetString(content);
    }

    public static Utf8StringParsingStrategy Default { get; } = new Utf8StringParsingStrategy();
}
