﻿using System.Collections.Generic;
 

namespace fASN1.NET.Tags;

public class Sequence : ITag
{
    public Sequence(List<ITag>? children = null)
    {
        Content = [];
        Children = children ?? new List<ITag>();
    }
    public int TagNumber { get; } = 48;
    public string TagName { get; } = "SEQUENCE";
    public int TagClass { get; } = 0;
    public bool IsConstructed => true;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
