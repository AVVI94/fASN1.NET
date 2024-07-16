using System.Collections.Generic;
 

namespace fASN1.NET.Tags;

public class Integer : ITag
{
    public Integer(byte[]? content = null)
    {
        Content = content ?? [];
        Children = new List<ITag>();
    }
    public int TagNumber { get; } = 2;
    public string TagName { get; } = "INTEGER";
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
