namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows BOOLEAN content: <c>False</c> for the single byte 0x00, <c>True</c> for any other single byte (X.690 §8.2.2;
/// DER uses 0xFF), and hexadecimal for content that is not one byte.
/// </summary>
public class BooleanParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        if (content.Length != 1)
            return OctetStringParsingStrategy.ToHex(content);
        return content[0] != 0 ? "True" : "False";
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>
    public static BooleanParsingStrategy Default { get; } = new BooleanParsingStrategy();
}
