using fASN1.NET.Oid;

namespace fASN1.NET.ContentParsing;
public class OidParsingStrategy : IContentParsingStrategy
{
    public string Parse(byte[] content)
    {
        var oid = OID.GetOrCreate(content);
        return $"{oid.Value}{(string.IsNullOrEmpty(oid.FriendlyName) ? "" : $", {oid.FriendlyName}")}{(string.IsNullOrEmpty(oid.Comment) ? "" : $" ({oid.Comment})")}";
    }

    public static OidParsingStrategy Default { get; } = new OidParsingStrategy();
}
