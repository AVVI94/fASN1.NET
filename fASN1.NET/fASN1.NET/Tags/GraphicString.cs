using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class GraphicString : TagBase
{
    public GraphicString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public override int TagNumber => 25;
    public override string TagName => "GraphicString";
    public override int TagClass => 0;
}
