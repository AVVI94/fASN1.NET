using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class TeletexString : TagBase
{
    public TeletexString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public override int TagNumber => 20;
    public override string TagName => "TeletexString";
    public override int TagClass => 0;
}
