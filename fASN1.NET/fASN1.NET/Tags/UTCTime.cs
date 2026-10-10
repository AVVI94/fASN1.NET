using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class UTCTime : TagBase
{
    public UTCTime(byte[]? content = null, IList<ITag>? children = null) : base(content, children)
    {
    }
    public UTCTime(DateTime time) : this(ConvertTime(time), null)
    {
    }

    private static byte[] ConvertTime(DateTime dateTime)
    {
        var str = dateTime.ToString("yyMMddHHmmss");
        if (int.Parse(str.Substring(0, 2)) >= 70)
        {
            str = "19" + str.Substring(2);
        }
        else
        {
            str = "20" + str.Substring(2);
        }
        return Encoding.ASCII.GetBytes(str);
    }

    public override int TagNumber => 23;
    public override string TagName => "UTCTime";
    public override int TagClass => 0;
}
