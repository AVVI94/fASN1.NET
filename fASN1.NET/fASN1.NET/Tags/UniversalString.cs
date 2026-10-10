using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class UniversalString : TagBase
{
    public UniversalString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public UniversalString(string text) : this(Encoding.UTF32.GetBytes(text))
    {
    }

    public override int TagNumber => 28;
    public override string TagName => "UniversalString";
    public override int TagClass => 0;
}
