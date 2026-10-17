using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 ObjectDescriptor type (universal 7).
/// </summary>
public class ObjectDescriptor : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public ObjectDescriptor(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 7;
    /// <inheritdoc/>
    public override string TagName => "ObjectDescriptor";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
