using fASN1.NET.Oid;

namespace fASN1.NET.ContentParsing;

/// <summary>
/// Shows OBJECT IDENTIFIER content in dotted form, followed by the friendly name and comment of a known OID.
/// </summary>
public class OidParsingStrategy : IContentParsingStrategy
{
    /// <inheritdoc/>
    public string Parse(byte[] content)
    {
        var oid = OID.GetOrCreate(content);
        return $"{oid.Value}{(string.IsNullOrEmpty(oid.FriendlyName) ? "" : $", {oid.FriendlyName}")}{(string.IsNullOrEmpty(oid.Comment) ? "" : $" ({oid.Comment})")}";
    }

    /// <summary>A shared instance; the strategy keeps no state.</summary>

    public static OidParsingStrategy Default { get; } = new OidParsingStrategy();
}
