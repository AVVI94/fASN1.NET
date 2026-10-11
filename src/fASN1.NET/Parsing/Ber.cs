using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// Bit masks, limits and identifier rules of the basic encoding rules (ITU-T X.690) used by the parser. The parser
/// decides the shape of the tree from the identifier octets alone, as X.690 defines it, whatever classes a factory
/// creates.
/// </summary>
internal static class Ber
{
    /// <summary>Identifier octet bit 6: the encoding is constructed, its contents are nested encodings (§8.1.2.5).</summary>
    public const int CONSTRUCTED_FLAG = 0x20;

    /// <summary>Identifier octet bits 5–1: the tag number, for numbers 0 to 30 (§8.1.2.3).</summary>
    public const int TAG_NUMBER_MASK = 0x1F;

    /// <summary>
    /// Identifier octet bits 5–1 all set: the tag number is in the high-tag-number form, in subsequent identifier
    /// octets (§8.1.2.4). Also the lowest tag number that form may hold.
    /// </summary>
    public const int HIGH_TAG_NUMBER_FORM = 0x1F;

    /// <summary>Bit 8 of a subsequent identifier octet: another one follows (§8.1.2.4.2 a).</summary>
    public const int TAG_NUMBER_CONTINUATION_FLAG = 0x80;

    /// <summary>Identifier octet of the end-of-contents octets <c>00 00</c>, which end indefinite-form contents (§8.1.5).</summary>
    public const int END_OF_CONTENTS_IDENTIFIER = 0x00;

    /// <summary>Identifier octet of a SET or SET OF (universal 17, constructed).</summary>
    public const int SET = 0x31;

    /// <summary>Bit 8 of the first length octet: the long form, bits 7–1 give the number of subsequent length octets (§8.1.3.5).</summary>
    public const int LONG_FORM_FLAG = 0x80;

    /// <summary>Bits 7–1 of the first length octet in the long form; 0 means the indefinite form (§8.1.3.6).</summary>
    public const int LENGTH_OCTET_COUNT_MASK = 0x7F;

    /// <summary>
    /// Most subsequent length octets accepted. Lengths above <see cref="int.MaxValue"/> are rejected anyway, but BER
    /// allows the long form padded with leading zero octets.
    /// </summary>
    public const int MAX_LENGTH_OCTETS = 6;

    /// <summary>The encoding is constructed: its contents are nested encodings.</summary>
    public static bool IsConstructed(int identifierOctet) => (identifierOctet & CONSTRUCTED_FLAG) != 0;

    /// <summary>
    /// A primitive universal OCTET STRING: its contents are tried as encapsulated encodings, as X.509 nests DER in
    /// OCTET STRINGs. A BIT STRING is never tried, so signatures and keys are not expanded by accident.
    /// </summary>
    public static bool MayEncapsulate(int identifierOctet) => identifierOctet == (int)Tag.OctetString;

    /// <summary>
    /// For a constructed string (BIT STRING, OCTET STRING, a character string or a time) the tag number of its
    /// fragments: 3 for a BIT STRING, 4 for the others (X.690 §8.6.4, §8.7.3, §8.23.6). 0 for any other identifier octet.
    /// </summary>
    public static int FragmentTagNumber(int identifierOctet)
    {
        if ((identifierOctet & ~TAG_NUMBER_MASK) != CONSTRUCTED_FLAG)
            return 0; // not universal and constructed
        return (identifierOctet & TAG_NUMBER_MASK) switch
        {
            (int)Tag.BitString => (int)Tag.BitString,
            (int)Tag.OctetString or (int)Tag.ObjectDescriptor or (int)Tag.UTF8String
                or (>= (int)Tag.NumericString and <= (int)Tag.UniversalString) or (int)Tag.BMPString => (int)Tag.OctetString,
            _ => 0,
        };
    }

    /// <summary>Asks <paramref name="factory"/> for the tag of a first identifier octet and tag number.</summary>
    public static ITag CreateTag(ITagFactory factory, int identifierOctet, int tagNumber)
        => factory.Create(identifierOctet >> 6, IsConstructed(identifierOctet), tagNumber);
}
