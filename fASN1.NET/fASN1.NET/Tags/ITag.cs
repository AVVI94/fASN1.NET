using System;
using System.Collections.Generic;
using System.Text;

namespace fASN1.NET.Tags;

public interface ITag
{
    public int TagNumber { get; }
    public string TagName { get; }
    public int TagClass { get; }
    public bool IsConstructed { get; }
    public bool IsUniversal { get; }
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
