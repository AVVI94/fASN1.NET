namespace fASN1.NET.ContentParsing;

/// <summary>
/// Turns the content bytes of a tag into readable text, for <see cref="Extensions.ContentToString"/> and
/// <see cref="Asn1Serializer.TagToString(Tags.ITag)"/>.
/// </summary>
public interface IContentParsingStrategy
{
    /// <summary>Converts content bytes to text.</summary>
    /// <param name="content">The content bytes of a tag.</param>
    /// <returns>The text.</returns>
    public string Parse(byte[] content);
}
