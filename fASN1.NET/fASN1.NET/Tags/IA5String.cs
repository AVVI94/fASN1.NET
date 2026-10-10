using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 IA5String type (universal 22): ASCII text.
/// </summary>
public class IA5String : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public IA5String(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <summary>Creates the tag from text, encoded as ASCII.</summary>
    /// <param name="text">The text.</param>
    public IA5String(string text) : this(Encoding.ASCII.GetBytes(text))
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 22;
    /// <inheritdoc/>
    public override string TagName => "IA5String";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
