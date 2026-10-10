using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class IA5String : TagBase
{
    public IA5String(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public IA5String(string text) : this(Encoding.ASCII.GetBytes(text))
    {
    }

    public override int TagNumber => 22;
    public override string TagName => "IA5String";
    public override int TagClass => 0;
}
