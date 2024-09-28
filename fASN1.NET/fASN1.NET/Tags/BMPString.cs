using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class BMPString : ITag
{
    public BMPString(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public BMPString(string value) : this(Encoding.BigEndianUnicode.GetBytes(value))
    {
    }

    public int TagNumber { get; } = 30;
    public string TagName { get; } = Tag.BMPString.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
