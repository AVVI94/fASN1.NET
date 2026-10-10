namespace fASN1.NET.Parsing;

/// <summary>
/// Bit masks and limits of the BER encoding (ITU-T X.690) used by the parser.
/// </summary>
internal static class Ber
{
    /// <summary>Identifier bit 6: the content is a sequence of nested elements.</summary>
    public const int ConstructedFlag = 0x20;

    /// <summary>
    /// Identifier bits 1–5: the tag number. When all five are set, the tag number continues in the
    /// following bytes (the multi-byte form), which this parser does not support.
    /// </summary>
    public const int TagNumberMask = 0x1F;

    /// <summary>Identifier of the end-of-contents marker that closes an indefinite-length element.</summary>
    public const int EndOfContents = 0x00;

    /// <summary>First length byte bit 8: the low seven bits give the number of length bytes that follow.</summary>
    public const int LongFormLengthFlag = 0x80;

    /// <summary>Mask of the number of length bytes in a long-form first length byte.</summary>
    public const int LengthOctetCountMask = 0x7F;

    /// <summary>
    /// Most length bytes accepted. Lengths above <see cref="int.MaxValue"/> are rejected anyway, but
    /// non-minimal encodings padded with leading zero bytes are allowed by BER.
    /// </summary>
    public const int MaxLengthOctets = 6;
}
