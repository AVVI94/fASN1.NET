﻿using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class Set : ITag
{
    public Set(List<ITag>? children = null)
    {
        Content = [];
        Children = children ?? new List<ITag>();
    }
    public int TagNumber { get; } = 49;
    public string TagName { get; } = Tag.Set.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => true;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
