using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class EmbeddedPdv : TagBase
{
    public EmbeddedPdv(List<ITag>? children = null) : base(null, children)
    {
    }
    public override int TagNumber => 0x2B; // universal 11, always constructed (like Sequence = 0x30)
    public override string TagName => "EmbeddedPDV";
    public override int TagClass => 0;
    public override bool IsConstructed => true;
}
