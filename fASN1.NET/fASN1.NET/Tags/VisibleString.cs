﻿using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class VisibleString : ITag
{
    public VisibleString(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }

    public VisibleString(string text) : this(Encoding.ASCII.GetBytes(text))
    {
    }

    public int TagNumber { get; } = 26;
    public string TagName { get; } = Tag.VisibleString.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }

    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
