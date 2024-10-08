﻿using System.Collections.Generic;
using System.Diagnostics;


namespace fASN1.NET.Tags;

[DebuggerDisplay("{DebuggerDisplay(),nq}")]
public class ObjectIdentifier : ITag
{
    public ObjectIdentifier(byte[]? content = null)
    {
        Content = content ?? [];
        Children = new List<ITag>();
    }
    public ObjectIdentifier(string oid) : this(Oid.OID.GetOrCreate(oid).ByteValue)
    {
    }
    public int TagNumber { get; } = 6;
    public string TagName { get; } = Tag.ObjectIdentifier.ToString2();
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }
    public ITag this[int index] { get => Children[index]; set => Children[index] = value; }

    private string DebuggerDisplay()
    {
        if (Content is not { Length: > 0 })
            return "ObjectIdentifier: (empty)";
        var o = Oid.OID.GetOrCreate(Content);
        return $"ObjectIdentifier: {o.Value}{(string.IsNullOrEmpty(o.FriendlyName) ? "" : $" ({o.FriendlyName})")}";
    }
}
