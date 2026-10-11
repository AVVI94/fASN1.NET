using System.Diagnostics;

namespace fASN1.NET.Tags;

/// <summary>
/// The ASN.1 OBJECT IDENTIFIER type (universal 6).
/// </summary>
[DebuggerDisplay("{DebuggerDisplay(),nq}")]
public class ObjectIdentifier : TagBase
{
    /// <summary>Creates the tag with the given content.</summary>
    /// <param name="content">The content bytes; empty when <see langword="null"/>.</param>
    public ObjectIdentifier(byte[]? content = null) : base(content)
    {
    }
    /// <summary>Creates the tag from an OID in dotted form, such as "2.5.29.15".</summary>
    /// <param name="oid">The OID.</param>
    public ObjectIdentifier(string oid) : this(Oid.OID.GetOrCreate(oid).ByteValue)
    {
    }
    /// <inheritdoc/>
    public override int TagNumber => 6;
    /// <inheritdoc/>
    public override string TagName => "ObjectIdentifier";
    /// <inheritdoc/>
    public override int TagClass => 0;
    /// <inheritdoc/>
    public override bool IsConstructed => false;

    private string DebuggerDisplay()
    {
        if (Content is not { Length: > 0 })
        {
            return "ObjectIdentifier: (empty)";
        }

        var o = Oid.OID.GetOrCreate(Content);
        return $"ObjectIdentifier: {o.Value}{(string.IsNullOrEmpty(o.FriendlyName) ? "" : $" ({o.FriendlyName})")}";
    }
}
