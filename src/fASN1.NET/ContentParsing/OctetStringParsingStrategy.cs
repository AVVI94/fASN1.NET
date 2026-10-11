using System;
using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows content as text when it is valid UTF-8 with no control characters other than tab, line feed and
/// carriage return; otherwise as uppercase hexadecimal. Also used for every non-universal tag.
/// </summary>
/// <remarks>
/// This is a display heuristic: binary data that happens to be printable UTF-8 is shown as text. Code that needs
/// the value should read <see cref="Tags.ITag.Content"/>, not this text.
/// </remarks>
public class OctetStringParsingStrategy : IContentParsingStrategy
{
    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>A shared instance; the strategy keeps no state.</summary>
    public static OctetStringParsingStrategy Default { get; } = new OctetStringParsingStrategy();

    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        string text;
        try
        {
            text = _strictUtf8.GetString(content);
        }
        catch (DecoderFallbackException)
        {
            // Invalid UTF-8 used to decode to U+FFFD, which passed as printable and turned binary data into garbage text.
            return ToHex(content);
        }

        foreach (var c in text)
        {
            if (char.IsControl(c) && c is not '\t' and not '\n' and not '\r')
            {
                return ToHex(content);
            }
        }
        return text;
    }

    /// <summary>The bytes as uppercase hexadecimal with no separators.</summary>
    internal static string ToHex(byte[] content) => BitConverter.ToString(content).Replace("-", "");
}
