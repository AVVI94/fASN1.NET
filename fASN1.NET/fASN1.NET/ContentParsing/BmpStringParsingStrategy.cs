using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Decodes BMPString content as UTF-16, big-endian.
/// </summary>
public class BmpStringParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return Encoding.BigEndianUnicode.GetString(content);
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static BmpStringParsingStrategy Default { get; } = new BmpStringParsingStrategy();
}
