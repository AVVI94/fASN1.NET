using System;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows content as Base64. Used for tags without a more specific strategy.
/// </summary>
public class Base64ParsingStrategy : IContentParsingStrategy
{
    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static Base64ParsingStrategy Default { get; } = new Base64ParsingStrategy();

    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return Convert.ToBase64String(content);
    }
}
