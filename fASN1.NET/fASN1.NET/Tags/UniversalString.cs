﻿using System.Collections.Generic;
using System.Text;


namespace fASN1.NET.Tags;

public class UniversalString : ITag
{
    public UniversalString(byte[]? content = null, IList<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public UniversalString(string text) : this(Encoding.UTF32.GetBytes(text))
    {
    }

    public int TagNumber { get; } = 28;
    public string TagName { get; } = Tag.UniversalString.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
