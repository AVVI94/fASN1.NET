using fASN1.NET.Factory;
using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// Bit masks, limits and identifier rules of the BER encoding (ITU-T X.690) used by the parser. The parser decides
/// the shape of the tree from the identifier byte alone, as X.690 defines it, whatever classes a factory creates.
/// </summary>
internal static class Ber
{
    /// <summary>Identifier bit 6: the content is a sequence of nested elements.</summary>
    public const int CONSTRUCTED_FLAG = 0x20;

    /// <summary>
    /// Identifier bits 1–5: the tag number. When all five are set, the tag number continues in the following bytes
    /// (the multi-byte form, for numbers of 31 and up).
    /// </summary>
    public const int TAG_NUMBER_MASK = 0x1F;

    /// <summary>Identifier of the end-of-contents marker that closes an indefinite-length element.</summary>
    public const int END_OF_CONTENTS = 0x00;

    /// <summary>Identifier of a SET or SET OF (universal 17, constructed).</summary>
    public const int SET = 0x31;

    /// <summary>First length byte bit 8: the low seven bits give the number of length bytes that follow.</summary>
    public const int LONG_FORM_LENGTH_FLAG = 0x80;

    /// <summary>Mask of the number of length bytes in a long-form first length byte.</summary>
    public const int LENGTH_OCTET_COUNT_MASK = 0x7F;

    /// <summary>
    /// Most length bytes accepted. Lengths above <see cref="int.MaxValue"/> are rejected anyway, but
    /// non-minimal encodings padded with leading zero bytes are allowed by BER.
    /// </summary>
    public const int MAX_LENGTH_OCTETS = 6;

    /// <summary>The content is a sequence of nested elements.</summary>
    public static bool IsConstructed(int identifier) => (identifier & CONSTRUCTED_FLAG) != 0;

    /// <summary>
    /// A primitive universal OCTET STRING: its content is tried as nested elements, because X.509 wraps DER
    /// structures in OCTET STRINGs. A BIT STRING is never tried, so signatures and keys are not expanded by accident.
    /// </summary>
    public static bool IsEncapsulationCandidate(int identifier) => identifier == (int)Tag.OctetString;

    /// <summary>
    /// For a constructed string (BIT STRING, OCTET STRING, a character string or a time) the tag number of its
    /// segments: 3 for a BIT STRING, 4 for the others (X.690 §8.6.4, §8.7.3, §8.23.6). 0 for any other identifier.
    /// </summary>
    public static int StringSegmentTagNumber(int identifier)
    {
        if ((identifier & ~TAG_NUMBER_MASK) != CONSTRUCTED_FLAG)
            return 0; // not universal and constructed
        return (identifier & TAG_NUMBER_MASK) switch
        {
            (int)Tag.BitString => (int)Tag.BitString,
            (int)Tag.OctetString or (int)Tag.ObjectDescriptor or (int)Tag.UTF8String
                or (>= (int)Tag.NumericString and <= (int)Tag.UniversalString) or (int)Tag.BMPString => (int)Tag.OctetString,
            _ => 0,
        };
    }

    /// <summary>Asks <paramref name="factory"/> for the tag of a first identifier byte and tag number.</summary>
    public static ITag CreateTag(ITagFactory factory, int identifier, int tagNumber)
        => factory.Create(identifier >> 6, IsConstructed(identifier), tagNumber);
}
