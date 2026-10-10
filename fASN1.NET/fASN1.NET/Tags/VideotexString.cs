using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class VideotexString : TagBase
{
    public VideotexString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public override int TagNumber => 21;
    public override string TagName => "VideotexString";
    public override int TagClass => 0;
}
