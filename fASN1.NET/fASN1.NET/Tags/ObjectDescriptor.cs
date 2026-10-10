using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class ObjectDescriptor : TagBase
{
    public ObjectDescriptor(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public override int TagNumber => 7;
    public override string TagName => "ObjectDescriptor";
    public override int TagClass => 0;
}
