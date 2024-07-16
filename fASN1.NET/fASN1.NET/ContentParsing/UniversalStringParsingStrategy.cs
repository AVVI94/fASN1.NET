using System.Text;

namespace fASN1.NET.ContentParsing;

public class UniversalStringParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return Encoding.UTF32.GetString(content);
    }

    public static UniversalStringParsingStrategy Default { get; } = new UniversalStringParsingStrategy();
}