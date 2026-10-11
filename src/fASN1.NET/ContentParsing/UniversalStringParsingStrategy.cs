using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Decodes UniversalString content, which is UCS-4 in big-endian byte order (X.690 §8.23.7).
/// </summary>
public class UniversalStringParsingStrategy : IContentParsingStrategy
{
    private static readonly UTF32Encoding _bigEndianUtf32 = new(bigEndian: true, byteOrderMark: false);

    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        // Encoding.UTF32 is little-endian, which turned every character into U+FFFD or a wrong one.
        return _bigEndianUtf32.GetString(content);
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>
    public static UniversalStringParsingStrategy Default { get; } = new UniversalStringParsingStrategy();
}
