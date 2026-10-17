using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 SET type (universal 17, identifier 0x31).
/// </summary>
public class Set : TagBase
{
    /// <summary>Creates the tag with the given children.</summary>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public Set(List<ITag>? children = null) : base(null, children)
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => (int)Tag.Set;
    /// <inheritdoc/>
    public override string TagName => "Set";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => true;
}
