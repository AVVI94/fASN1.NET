using System.Collections.Generic;
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
    public int TagNumber { get; } = 6;
    public string TagName { get; } = "OBJECT_IDENTIFIER";
    public int TagClass { get; } = 0;
    public bool IsConstructed { get; } = false;
    public bool IsUniversal { get; } = true;
    public bool IsEoc { get; }
    public IList<ITag> Children { get; }
    public byte[] Content { get; set; }

    private string DebuggerDisplay()
    {
        var o = Oid.OID.GetOrCreate(Content);
        return $"ObjectIdentifier: {o.Value}{(string.IsNullOrEmpty(o.FriendlyName) ? "" : $" ({o.FriendlyName})")}";
    }
}
