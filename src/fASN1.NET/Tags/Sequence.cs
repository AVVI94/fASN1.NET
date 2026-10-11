using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 SEQUENCE type (universal 16, identifier 0x30).
/// </summary>
public class Sequence : TagBase
{
    /// <summary>Creates the tag with the given children.</summary>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public Sequence(List<ITag>? children = null) : base(null, children)
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => (int)Tag.Sequence;
    /// <inheritdoc/>
    public override string TagName => "Sequence";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => true;
}
