﻿using System;
using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class BooleanTag : ITag
{
    public BooleanTag(byte[]? content = null)
    {
        Content = content ?? [];
        Children = new List<ITag>();
    }
    public BooleanTag(bool value) : this(BitConverter.GetBytes(value))
    {
    }
    public int TagNumber { get; } = 1;
    public string TagName { get; } = Tag.Boolean.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
