using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 BMPString type (universal 30): text in UTF-16, big-endian.
/// </summary>
public class BMPString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public BMPString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <summary>Creates the tag from text, encoded as UTF-16, big-endian.</summary>
    /// <param name="text">The text.</param>
    public BMPString(string text) : this(Encoding.BigEndianUnicode.GetBytes(text))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 30;
    /// <inheritdoc/>
    public override string TagName => "BMPString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
