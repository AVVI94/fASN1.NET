﻿using System.Collections.Generic;


namespace fASN1.NET.Tags;

public class ContextSpecific_2 : ITag
{
    public ContextSpecific_2(byte[]? content = null, IList<ITag>? children = null)
    {
        TagNumber = 162;
        TagName = $"[2]";
        Content = content ?? [];
        Children = children ?? new List<ITag>();
    }
    public int TagNumber { get; }
    public string TagName { get; }
    public int TagClass { get; } = 2;
    public bool IsConstructed => Content.Length == 0 && Children.Count > 0;
    public bool IsUniversal { get; }
    public bool IsEoc { get; } = false;
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
}
