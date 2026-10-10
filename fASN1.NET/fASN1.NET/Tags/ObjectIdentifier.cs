using System.Diagnostics;

namespace fASN1.NET.Tags;

[DebuggerDisplay("{DebuggerDisplay(),nq}")]
public class ObjectIdentifier : TagBase
{
    public ObjectIdentifier(byte[]? content = null) : base(content)
    {
    }
    public ObjectIdentifier(string oid) : this(Oid.OID.GetOrCreate(oid).ByteValue)
    {
    }
    public override int TagNumber => 6;
    public override string TagName => "ObjectIdentifier";
    public override int TagClass => 0;
    public override bool IsConstructed => false;

    private string DebuggerDisplay()
    {
        if (Content is not { Length: > 0 })
            return "ObjectIdentifier: (empty)";
        var o = Oid.OID.GetOrCreate(Content);
        return $"ObjectIdentifier: {o.Value}{(string.IsNullOrEmpty(o.FriendlyName) ? "" : $" ({o.FriendlyName})")}";
    }
}
