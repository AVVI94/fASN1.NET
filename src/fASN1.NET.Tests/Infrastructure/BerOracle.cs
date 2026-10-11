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
/// </list>
/// </remarks>
internal sealed record OracleNode(int TagClass, bool IsConstructed, int TagNumber, byte[] Content, List<OracleNode> Children)
{
    public static OracleNode Decode(ReadOnlySpan<byte> data) => ReadElement(data, AsnEncodingRules.BER, out _);

    private static OracleNode ReadElement(ReadOnlySpan<byte> src, AsnEncodingRules rules, out int consumed)
    {
        AsnDecoder.ReadEncodedValue(src, rules, out var contentOffset, out var contentLength, out consumed);
        var tag = Asn1Tag.Decode(src, out _);
        var (tagClass, isConstructed, number) = ((int)tag.TagClass >> 6, tag.IsConstructed, tag.TagValue);
        var content = src.Slice(contentOffset, contentLength);

        // Inside an OCTET STRING, a SET must also be in DER order (X.690 §11.6), which ReadSetOf checks.
        if (rules == AsnEncodingRules.DER && tagClass == 0 && isConstructed && number == 17)
            new AsnReader(src.Slice(0, consumed).ToArray(), AsnEncodingRules.DER).ReadSetOf();

        if (tagClass == 0 && isConstructed && IsStringType(number))
        {
            // DER has no constructed strings, so inside an OCTET STRING one makes the probe fail.
            if (rules == AsnEncodingRules.DER)
                throw new AsnContentException("Constructed string in DER");
            var joined = Join(src.Slice(0, consumed), number);
            if (number == 4 && TryReadEncapsulated(joined, out var joinedChildren))
                return new OracleNode(tagClass, false, number, [], joinedChildren);
            return new OracleNode(tagClass, false, number, joined, []);
        }

        if (isConstructed)
            return new OracleNode(tagClass, isConstructed, number, [], ReadAll(content, rules));

        if (tagClass == 0 && number == 4 && TryReadEncapsulated(content, out var encapsulated))
            return new OracleNode(tagClass, isConstructed, number, [], encapsulated);

        return new OracleNode(tagClass, isConstructed, number, content.ToArray(), []);
    }

    /// <summary>BIT STRING, OCTET STRING, the restricted character strings and the times may be constructed in BER.</summary>
    private static bool IsStringType(int number) => number is 3 or 4 or 7 or 12 or (>= 18 and <= 28) or 30;

    /// <summary>Appends the bits of every primitive BIT STRING segment of <paramref name="encoded"/>, without its unused-bits byte.</summary>
    private static void AppendSegmentData(ReadOnlySpan<byte> encoded, List<byte> data)
    {
        AsnDecoder.ReadEncodedValue(encoded, AsnEncodingRules.BER, out var offset, out var length, out _);
        if ((encoded[0] & 0x20) == 0)
        {
            data.AddRange(encoded.Slice(offset + 1, length - 1).ToArray());
            return;
        }
        var content = encoded.Slice(offset, length);
        while (!content.IsEmpty && !(content[0] == 0 && content[1] == 0))
        {
            AsnDecoder.ReadEncodedValue(content, AsnEncodingRules.BER, out _, out _, out var consumed);
            AppendSegmentData(content.Slice(0, consumed), data);
            content = content.Slice(consumed);
        }
    }

    /// <summary>The value of a constructed string, as System.Formats.Asn1 joins it.</summary>
    private static byte[] Join(ReadOnlySpan<byte> encoded, int number)
    {
        if (number == 3)
        {
            // .NET validates the segments, but clears the unused bits; like a primitive BIT STRING, the value keeps
            // them as encoded, so the bits are taken from the segments themselves.
            AsnDecoder.ReadBitString(encoded, AsnEncodingRules.BER, out var unusedBits, out _);
            var data = new List<byte>();
            AppendSegmentData(encoded, data);
            return [(byte)unusedBits, .. data];
        }
        // The segments of every other string type are OCTET STRINGs (X.690 §8.23.6), but .NET reads only OCTET STRINGs
        // that way, so a copy relabelled as a constructed OCTET STRING is read instead.
        byte[] relabelled = [0x24, .. encoded[1..]];
        return AsnDecoder.ReadOctetString(relabelled, AsnEncodingRules.BER, out _);
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
            return children.Count > 0 && !children.Any(c => c.IsEoc);
        }
        catch (AsnContentException)
        {
            return false;
        }
    }

    public bool IsEoc => TagClass == 0 && !IsConstructed && TagNumber == 0;

    public int CountNodes() => 1 + Children.Sum(c => c.CountNodes());

    /// <summary>
    /// Compares the parsed tree against this oracle node: tag identity, CLR type, class,
    /// constructed flag, content bytes and the complete ordered list of children.
    /// </summary>
    public void AssertMatches(ITag actual) => AssertMatches(actual, "root");

    private void AssertMatches(ITag actual, string path)
    {
        Check(path, "type", TagIdentity.ExpectedType(TagClass, IsConstructed, TagNumber), actual.GetType());
        Check(path, nameof(ITag.TagName), TagIdentity.ExpectedName(TagClass, IsConstructed, TagNumber), actual.TagName);
        Check(path, nameof(ITag.TagClass), TagClass, actual.TagClass);
        Check(path, nameof(ITag.IsUniversal), TagClass == 0, actual.IsUniversal);
        Check(path, nameof(ITag.TagNumber), TagNumber, actual.TagNumber);
        Check(path, nameof(ITag.IsEoc), IsEoc, actual.IsEoc);
        // An OCTET STRING read as encapsulated children reports itself constructed, though its encoding is primitive.
        Check(path, nameof(ITag.IsConstructed), IsConstructed || Children.Count > 0, actual.IsConstructed);
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
    /// <summary>Universal types by their single identifier byte.</summary>
    private static readonly Dictionary<int, (Type Type, string Name)> _universal = new()
    {
        [0x00] = (typeof(Eoc), "Eoc"),
        [0x01] = (typeof(BooleanTag), "Boolean"),
        [0x02] = (typeof(Integer), "Integer"),
        [0x03] = (typeof(BitString), "BitString"),
        [0x04] = (typeof(OctetString), "OctetString"),
        [0x05] = (typeof(Null), "Null"),
        [0x06] = (typeof(ObjectIdentifier), "ObjectIdentifier"),
        [0x07] = (typeof(ObjectDescriptor), "ObjectDescriptor"),
        [0x08] = (typeof(External), "External"),
        [0x09] = (typeof(Real), "Real"),
        [0x0A] = (typeof(Enumerated), "Enumerated"),
        [0x0C] = (typeof(Utf8String), "UTF8String"),
        [0x12] = (typeof(NumericString), "NumericString"),
        [0x13] = (typeof(PrintableString), "PrintableString"),
        [0x14] = (typeof(TeletexString), "TeletexString"),
        [0x15] = (typeof(VideotexString), "VideotexString"),
        [0x16] = (typeof(IA5String), "IA5String"),
        [0x17] = (typeof(UTCTime), "UTCTime"),
        [0x18] = (typeof(GeneralizedTime), "GeneralizedTime"),
        [0x19] = (typeof(GraphicString), "GraphicString"),
        [0x1A] = (typeof(VisibleString), "VisibleString"),
        [0x1B] = (typeof(GeneralString), "GeneralString"),
        [0x1C] = (typeof(UniversalString), "UniversalString"),
        [0x1E] = (typeof(BMPString), "BMPString"),
        [0x30] = (typeof(Sequence), "Sequence"),
        [0x31] = (typeof(Tags.Set), "Set"),
        [0x2B] = (typeof(EmbeddedPdv), "EmbeddedPDV"),
    };

    public static IEnumerable<int> KnownUniversalIdentifiers => _universal.Keys;

    public static Type ExpectedType(int id) => ExpectedType(id >> 6, (id & 0x20) != 0, id & 0x1F);

    public static string ExpectedName(int id) => ExpectedName(id >> 6, (id & 0x20) != 0, id & 0x1F);

    public static int ExpectedTagNumber(int id) => id & 0x1F;

    /// <summary>The single identifier byte of a tag number below 31, or -1 for the multi-byte form.</summary>
    private static int LowFormIdentifier(int tagClass, bool isConstructed, int number)
        => number < 31 ? (tagClass << 6) | (isConstructed ? 0x20 : 0) | number : -1;

    public static Type ExpectedType(int tagClass, bool isConstructed, int number) => tagClass switch
    {
        0 => _universal.TryGetValue(LowFormIdentifier(tagClass, isConstructed, number), out var u) ? u.Type : typeof(Unknown),
        1 => typeof(Application),
        2 => number switch
        {
            0 => typeof(ContextSpecific_0),
            1 => typeof(ContextSpecific_1),
            2 => typeof(ContextSpecific_2),
            3 => typeof(ContextSpecific_3),
            _ => typeof(ContextSpecificTag),
        },
        _ => typeof(Private),
    };

    public static string ExpectedName(int tagClass, bool isConstructed, int number) => tagClass switch
    {
        0 => _universal.TryGetValue(LowFormIdentifier(tagClass, isConstructed, number), out var u) ? u.Name : $"Unknown_{number}",
        1 => $"Application_{number}",
        2 => $"[{number}]",
        _ => $"Private_{number}",
    };
}
