using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Decodes UTF8String content.
/// </summary>
public class Utf8StringParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return Encoding.UTF8.GetString(content);
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static Utf8StringParsingStrategy Default { get; } = new Utf8StringParsingStrategy();
}
