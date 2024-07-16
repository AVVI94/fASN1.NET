using System;

namespace fASN1.NET.ContentParsing;

public class Base64ParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        return Convert.ToBase64String(content);
    }

    public static Base64ParsingStrategy Default { get; } = new Base64ParsingStrategy();
}
