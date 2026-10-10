using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class VisibleString : TagBase
{
    public VisibleString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public VisibleString(string text) : this(Encoding.ASCII.GetBytes(text))
    {
    }

    public override int TagNumber => 26;
    public override string TagName => "VisibleString";
    public override int TagClass => 0;
}
