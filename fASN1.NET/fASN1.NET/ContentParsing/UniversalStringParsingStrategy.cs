using System.Text;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Decodes UniversalString content with <see cref="System.Text.Encoding.UTF32"/>.
/// </summary>
public class UniversalStringParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        return Encoding.UTF32.GetString(content);
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static UniversalStringParsingStrategy Default { get; } = new UniversalStringParsingStrategy();
}