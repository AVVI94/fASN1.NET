namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows BOOLEAN content: <c>True</c> for the single byte 0xFF, <c>False</c> for anything else.
/// </summary>
public class BooleanParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return content.Length == 1 && content[0] == 0xff ? "True" : "False";
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static BooleanParsingStrategy Default { get; } = new BooleanParsingStrategy();
}
