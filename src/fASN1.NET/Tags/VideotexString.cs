using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 VideotexString type (universal 21).
/// </summary>
public class VideotexString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public VideotexString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 21;
    /// <inheritdoc/>
    public override string TagName => "VideotexString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
