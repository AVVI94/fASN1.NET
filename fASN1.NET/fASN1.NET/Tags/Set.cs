using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class Set : TagBase
{
    public Set(List<ITag>? children = null) : base(null, children)
    {
    }
    public override int TagNumber => 49;
    public override string TagName => "Set";
    public override int TagClass => 0;
    public override bool IsConstructed => true;
}
