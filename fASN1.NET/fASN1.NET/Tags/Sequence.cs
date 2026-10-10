using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class Sequence : TagBase
{
    public Sequence(List<ITag>? children = null) : base(null, children)
    {
    }
    public override int TagNumber => 48;
    public override string TagName => "Sequence";
    public override int TagClass => 0;
    public override bool IsConstructed => true;
}
