using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class Real : ITag
{
    public Real(byte[]? content = null)
    {
        Content = content ?? [];
        Children = new List<ITag>();
    }
    public int TagNumber { get; } = 9;
    public string TagName { get; } = Tag.Real.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
