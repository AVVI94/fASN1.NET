using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class OctetString : TagBase
{
    public OctetString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public OctetString(string text) : this(Encoding.UTF8.GetBytes(text))
    {
    }

    public override int TagNumber => 4;
    public override string TagName => "OctetString";
    public override int TagClass => 0;
}
