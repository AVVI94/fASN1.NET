using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class BMPString : TagBase
{
    public BMPString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public BMPString(string text) : this(Encoding.BigEndianUnicode.GetBytes(text))
    {
    }

    public override int TagNumber => 30;
    public override string TagName => "BMPString";
    public override int TagClass => 0;
}
