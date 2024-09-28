using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class UTCTime : ITag
{
    public UTCTime(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
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

    public int TagNumber { get; } = 23;
    public string TagName { get; } = Tag.UTCTime.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
