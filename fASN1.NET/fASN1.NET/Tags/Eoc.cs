﻿using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class Eoc : ITag
{
    public Eoc()
    {
        Content = [];
        Children = new List<ITag>();
    }
    public int TagNumber { get; } = 0;
    public string TagName { get; } = Tag.Eoc.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; } = true;
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
