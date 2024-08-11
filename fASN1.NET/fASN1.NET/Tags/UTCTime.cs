using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class UTCTime : ITag
{
    public UTCTime(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public int TagNumber { get; } = 23;
    public string TagName { get; } = Tag.UTCTime.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
