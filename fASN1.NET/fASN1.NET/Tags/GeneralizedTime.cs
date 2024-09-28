using System;
using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class GeneralizedTime : ITag
{
    public GeneralizedTime(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public GeneralizedTime(DateTime time)
    {
        var val = time.ToString("yyyyMMddHHmmss.f");
        if (time.Kind == DateTimeKind.Utc)
        {
            val += "Z";
        }
        Content = Encoding.ASCII.GetBytes(val);
        Children = [];
    }
    public int TagNumber { get; } = 24;
    public string TagName { get; } = Tag.GeneralizedTime.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
