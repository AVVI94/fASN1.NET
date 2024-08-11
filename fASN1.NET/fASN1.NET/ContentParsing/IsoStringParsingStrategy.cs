using System.Text;

namespace fASN1.NET.ContentParsing;
public class IsoStringParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return Encoding.ASCII.GetString(content);
    }

    public static IsoStringParsingStrategy Default { get; } = new IsoStringParsingStrategy();
}
