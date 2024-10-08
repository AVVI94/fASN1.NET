﻿using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class Null : ITag
{
    public Null()
    {
        Children = new List<ITag>();
        Content = [];
    }
    public int TagNumber { get; } = 5;
    public string TagName { get; } = Tag.Null.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
