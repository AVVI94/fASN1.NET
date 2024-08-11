using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class UniversalTag : ITag
{
    public UniversalTag(int tagNumber, byte[]? content = null, IList<ITag>? children = null)
    {
        TagNumber = tagNumber;
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public int TagNumber { get; }
    public string TagName => "Universal_" + TagNumber;
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
