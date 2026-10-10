using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class Utf8String : TagBase
{
    public Utf8String(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public Utf8String(string text) : this(Encoding.UTF8.GetBytes(text))
    {
    }

    public override int TagNumber => 12;
    public override string TagName => "UTF8String";
    public override int TagClass => 0;
}
