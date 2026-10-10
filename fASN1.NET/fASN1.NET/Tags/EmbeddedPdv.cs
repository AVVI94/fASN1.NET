using System.Collections.Generic;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 EMBEDDED PDV type (universal 11), which is always constructed (identifier 0x2B).
/// </summary>
public class EmbeddedPdv : TagBase
{
    /// <summary>Creates the tag with the given children.</summary>
    /// <param name="children">The child tags, used by reference; none when <see langword="null"/>.</param>
    public EmbeddedPdv(List<ITag>? children = null) : base(null, children)
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 0x2B; // universal 11, always constructed (like Sequence = 0x30)
    /// <inheritdoc/>
    public override string TagName => "EmbeddedPDV";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => true;
}
