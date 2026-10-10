using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 OCTET STRING type (universal 4). The parser shows its content as children when it holds complete DER elements.
/// </summary>
public class OctetString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public OctetString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <summary>Creates the tag from text, encoded as UTF-8.</summary>
    /// <param name="text">The text.</param>
    public OctetString(string text) : this(Encoding.UTF8.GetBytes(text))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 4;
    /// <inheritdoc/>
    public override string TagName => "OctetString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
