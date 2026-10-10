using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class External : TagBase
{
    public External(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public override int TagNumber => 8;
    public override string TagName => "External";
    public override int TagClass => 0;
}
