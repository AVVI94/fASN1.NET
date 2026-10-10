using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class GeneralizedTime : TagBase
{
    public GeneralizedTime(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    public GeneralizedTime(DateTime time) : base(Encode(time))
    {
    }

    private static byte[] Encode(DateTime time)
    {
        var val = time.ToString("yyyyMMddHHmmss.f");
        if (time.Kind == DateTimeKind.Utc)
        {
            val += "Z";
        }
        return Encoding.ASCII.GetBytes(val);
    }

    public override int TagNumber => 24;
    public override string TagName => "GeneralizedTime";
    public override int TagClass => 0;
}
