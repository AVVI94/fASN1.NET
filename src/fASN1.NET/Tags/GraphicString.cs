using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 GraphicString type (universal 25).
/// </summary>
public class GraphicString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public GraphicString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 25;
    /// <inheritdoc/>
    public override string TagName => "GraphicString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
