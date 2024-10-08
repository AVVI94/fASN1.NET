﻿using System.Collections.Generic;

namespace fASN1.NET.Tags;

public class Private : ITag
{
    public Private(int tagNumber, byte[]? content = null, List<ITag>? children = null)
    {
        Content = content ?? [];
        Children = children ?? [];
        TagClass = tagNumber >> 6;
        IsConstructed = (tagNumber & 0x20) != 0;
        TagName = $"Private_{tagNumber & 0x1F}";
    }
    public int TagNumber { get; }
    public string TagName { get; }
    public int TagClass { get; }
    public bool IsConstructed { get; }
    public bool IsUniversal => TagClass == 0x00;
    public bool IsEoc => TagClass == 0x00 && TagNumber == 0x00;
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }
}
