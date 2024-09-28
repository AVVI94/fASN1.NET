using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class Utf8String : ITag
{
    public Utf8String(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public Utf8String(string text) : this(System.Text.Encoding.UTF8.GetBytes(text))
    {
    }
    public int TagNumber { get; } = 12;
    public string TagName { get; } = Tag.UTF8String.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
