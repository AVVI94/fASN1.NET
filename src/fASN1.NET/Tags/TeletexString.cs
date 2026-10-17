using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 TeletexString type (universal 20).
/// </summary>
public class TeletexString : TagBase
{
    /// <summary>Creates the tag with the given content or children.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public TeletexString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    /// <inheritdoc/>
    public override int TagNumber => 20;
    /// <inheritdoc/>
    public override string TagName => "TeletexString";
    /// <inheritdoc/>
    public override int TagClass => 0;
}
