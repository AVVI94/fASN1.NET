using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public class GeneralString : TagBase
{
    public GeneralString(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }

    public GeneralString(string text) : this(Encoding.ASCII.GetBytes(text))
    {
    }

    public override int TagNumber => 27;
    public override string TagName => "GeneralString";
    public override int TagClass => 0;
}
