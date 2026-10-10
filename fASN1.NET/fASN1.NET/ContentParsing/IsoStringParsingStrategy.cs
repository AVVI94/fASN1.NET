using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Decodes text content as ASCII.
/// </summary>
public class IsoStringParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return Encoding.ASCII.GetString(content);
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static IsoStringParsingStrategy Default { get; } = new IsoStringParsingStrategy();
}
