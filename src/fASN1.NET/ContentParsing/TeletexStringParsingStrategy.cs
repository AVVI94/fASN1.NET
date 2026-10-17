using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Decodes TeletexString (T61String) content. T.61 itself is rarely what encoders write: in practice the bytes are
/// Latin-1 or UTF-8. As in .NET's <c>X500DistinguishedName</c>, content that is valid UTF-8 is decoded as UTF-8 and
/// anything else as Latin-1 (ISO 8859-1).
/// </summary>
public class TeletexStringParsingStrategy : IContentParsingStrategy
{
    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>A shared instance; the strategy keeps no state.</summary>
    public static TeletexStringParsingStrategy Default { get; } = new TeletexStringParsingStrategy();

    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        try
        {
            return _strictUtf8.GetString(content);
        }
        catch (DecoderFallbackException)
        {
            // Latin-1 maps every byte to the character with the same code.
            var chars = new char[content.Length];
            for (int i = 0; i < content.Length; i++)
            {
                chars[i] = (char)content[i];
            }
            return new string(chars);
        }
    }
}
