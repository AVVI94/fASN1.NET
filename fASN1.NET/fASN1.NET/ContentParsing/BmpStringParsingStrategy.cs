using System.Text;

namespace fASN1.NET.ContentParsing;

public class BmpStringParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return Encoding.BigEndianUnicode.GetString(content);
    }

    public static BmpStringParsingStrategy Default { get; } = new BmpStringParsingStrategy();
}
