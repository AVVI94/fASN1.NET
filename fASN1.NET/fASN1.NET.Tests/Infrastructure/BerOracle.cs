using System.Formats.Asn1;
using fASN1.NET.Tags;
using Xunit.Sdk;

namespace fASN1.NET.Tests.Infrastructure;

/// <summary>
/// Independent description of what the parsed tree should look like, built with
/// <see cref="AsnDecoder"/> from System.Formats.Asn1 instead of fASN1.NET code.
/// </summary>
/// <remarks>
/// The oracle encodes these library decisions on top of plain BER:
/// <list type="bullet">
/// <item>A primitive universal OCTET STRING whose content is a complete run of valid elements with DER lengths
/// (definite, minimal; none of them EOC) and every SET in DER order is shown with those elements as children; otherwise it is a leaf.</item>
/// <item>A BIT STRING is always a leaf; its unused-bits byte stays in Content.</item>
/// <item>Only single-byte identifiers are supported.</item>
/// </list>
/// </remarks>
internal sealed record OracleNode(byte Identifier, byte[] Content, List<OracleNode> Children)
{
    public static OracleNode Decode(ReadOnlySpan<byte> data) => ReadElement(data, AsnEncodingRules.BER, out _);

    private static OracleNode ReadElement(ReadOnlySpan<byte> src, AsnEncodingRules rules, out int consumed)
    {
        if ((src[0] & 0x1F) == 0x1F)
            throw new AsnContentException("Multi-byte identifiers are outside the supported subset");
        AsnDecoder.ReadEncodedValue(src, rules, out var contentOffset, out var contentLength, out consumed);
        var identifier = src[0];
        var content = src.Slice(contentOffset, contentLength);

        // Inside an OCTET STRING, a SET must also be in DER order (X.690 §11.6), which ReadSetOf checks.
        if (rules == AsnEncodingRules.DER && identifier == 0x31)
            new AsnReader(src.Slice(0, consumed).ToArray(), AsnEncodingRules.DER).ReadSetOf();

        if ((identifier & 0x20) != 0)
            return new OracleNode(identifier, [], ReadAll(content, rules));

        if (identifier == 0x04 && TryReadEncapsulated(content, out var encapsulated))
            return new OracleNode(identifier, [], encapsulated);

        return new OracleNode(identifier, content.ToArray(), []);
    }

    private static List<OracleNode> ReadAll(ReadOnlySpan<byte> content, AsnEncodingRules rules)
    {
        var list = new List<OracleNode>();
        while (!content.IsEmpty)
        {
            list.Add(ReadElement(content, rules, out var consumed));
            content = content.Slice(consumed);
        }
        return list;
    }

    private static bool TryReadEncapsulated(ReadOnlySpan<byte> content, out List<OracleNode> children)
    {
        children = [];
        try
        {
            // DER lengths only: the library re-encodes encapsulated children with DER lengths, so any other
            // form would change the OCTET STRING value on Serialize.
            children = ReadAll(content, AsnEncodingRules.DER);
            // EOC cannot be content, so a direct EOC child means this is not a run of elements.
            return children.Count > 0 && !children.Any(c => c.Identifier == 0x00);
        }
        catch (AsnContentException)
        {
            return false;
        }
    }

    public int CountNodes() => 1 + Children.Sum(c => c.CountNodes());

    /// <summary>
    /// Compares the parsed tree against this oracle node: tag identity, CLR type, class,
    /// constructed flag, content bytes and the complete ordered list of children.
    /// </summary>
    public void AssertMatches(ITag actual) => AssertMatches(actual, "root");

    private void AssertMatches(ITag actual, string path)
    {
        var id = Identifier;
        Check(path, "type", TagIdentity.ExpectedType(id), actual.GetType());
        Check(path, nameof(ITag.TagName), TagIdentity.ExpectedName(id), actual.TagName);
        Check(path, nameof(ITag.TagClass), id >> 6, actual.TagClass);
        Check(path, nameof(ITag.IsUniversal), id >> 6 == 0, actual.IsUniversal);
        Check(path, nameof(ITag.TagNumber), TagIdentity.ExpectedTagNumber(id), actual.TagNumber);
        Check(path, nameof(ITag.IsEoc), id == 0x00, actual.IsEoc);
        Check(path, nameof(ITag.Content), Convert.ToHexString(Content), Convert.ToHexString(actual.Content));
        Check(path, "Children.Count", Children.Count, actual.Children.Count);
        for (int i = 0; i < Children.Count; i++)
            Children[i].AssertMatches(actual.Children[i], $"{path}/{i}");
    }

    private static void Check<T>(string path, string property, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new XunitException($"Oracle mismatch at {path}, {property}:{Environment.NewLine}  expected: {expected}{Environment.NewLine}  actual:   {actual}");
    }
}

/// <summary>
/// The tag identity contract written out independently of <c>DefaultTagFactory</c>.
/// </summary>
internal static class TagIdentity
{
    private static readonly Dictionary<int, (Type Type, string Name, int TagNumber)> _universal = new()
    {
        [0x00] = (typeof(Eoc), "Eoc", 0),
        [0x01] = (typeof(BooleanTag), "Boolean", 1),
        [0x02] = (typeof(Integer), "Integer", 2),
        [0x03] = (typeof(BitString), "BitString", 3),
        [0x04] = (typeof(OctetString), "OctetString", 4),
        [0x05] = (typeof(Null), "Null", 5),
        [0x06] = (typeof(ObjectIdentifier), "ObjectIdentifier", 6),
        [0x07] = (typeof(ObjectDescriptor), "ObjectDescriptor", 7),
        [0x08] = (typeof(External), "External", 8),
        [0x09] = (typeof(Real), "Real", 9),
        [0x0A] = (typeof(Enumerated), "Enumerated", 10),
        [0x0C] = (typeof(Utf8String), "UTF8String", 12),
        [0x12] = (typeof(NumericString), "NumericString", 18),
        [0x13] = (typeof(PrintableString), "PrintableString", 19),
        [0x14] = (typeof(TeletexString), "TeletexString", 20),
        [0x15] = (typeof(VideotexString), "VideotexString", 21),
        [0x16] = (typeof(IA5String), "IA5String", 22),
        [0x17] = (typeof(UTCTime), "UTCTime", 23),
        [0x18] = (typeof(GeneralizedTime), "GeneralizedTime", 24),
        [0x19] = (typeof(GraphicString), "GraphicString", 25),
        [0x1A] = (typeof(VisibleString), "VisibleString", 26),
        [0x1B] = (typeof(GeneralString), "GeneralString", 27),
        [0x1C] = (typeof(UniversalString), "UniversalString", 28),
        [0x1E] = (typeof(BMPString), "BMPString", 30),
        [0x30] = (typeof(Sequence), "Sequence", 48),
        [0x31] = (typeof(Tags.Set), "Set", 49),
        [0x2B] = (typeof(EmbeddedPdv), "EmbeddedPDV", 43),
    };

    public static IEnumerable<int> KnownUniversalIdentifiers => _universal.Keys;

    public static Type ExpectedType(int id) => (id >> 6) switch
    {
        0 => _universal.TryGetValue(id, out var u) ? u.Type : typeof(Unknown),
        1 => typeof(Application),
        2 => (id & 0x1F) switch
        {
            0 => typeof(ContextSpecific_0),
            1 => typeof(ContextSpecific_1),
            2 => typeof(ContextSpecific_2),
            3 => typeof(ContextSpecific_3),
            _ => typeof(ContextSpecificTag),
        },
        _ => typeof(Private),
    };

    public static string ExpectedName(int id) => (id >> 6) switch
    {
        0 => _universal.TryGetValue(id, out var u) ? u.Name : $"Unknown_{id & 0x1F}",
        1 => $"Application_{id & 0x1F}",
        2 => $"[{id & 0x1F}]",
        _ => $"Private_{id & 0x1F}",
    };

    /// <summary>
    /// Known universal classes report their fixed number; every other class reports the identifier byte.
    /// </summary>
    public static int ExpectedTagNumber(int id) => (id >> 6) == 0 && _universal.TryGetValue(id, out var u) ? u.TagNumber : id;
}
