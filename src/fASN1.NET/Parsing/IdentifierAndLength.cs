namespace fASN1.NET.Parsing;

/// <summary>
/// The identifier octets and length octets of an encoding (X.690 §8.1.2, §8.1.3), and where its contents octets
/// are: everything about the encoding except the contents themselves.
/// </summary>
internal struct IdentifierAndLength
{
    /// <summary>Offset of the first identifier octet, where the encoding starts.</summary>
    public long IdentifierStart;

    /// <summary>
    /// The first identifier octet: class (bits 8–7), primitive or constructed (bit 6), and the tag number in bits
    /// 5–1, or the high-tag-number marker 11111.
    /// </summary>
    public int IdentifierOctet;

    /// <summary>The tag number, also when it is in the high-tag-number form.</summary>
    public int TagNumber;

    /// <summary>Offset of the first contents octet.</summary>
    public long ContentsStart;

    /// <summary>Number of contents octets in the definite form; <see langword="null"/> in the indefinite form.</summary>
    public long? ContentsLength;

    /// <summary>The length is in the definite form with the fewest octets, as DER requires (X.690 §10.1).</summary>
    public bool IsShortestDefiniteForm;

    /// <summary>The length is in the indefinite form: the contents end with end-of-contents octets.</summary>
    public readonly bool IsIndefiniteForm => ContentsLength is null;

    /// <summary>Offset just past the contents octets, in the definite form.</summary>
    public readonly long ContentsEnd => ContentsStart + ContentsLength!.Value;

    /// <summary>
    /// The identifier is that of the end-of-contents octets, <c>00</c>; whether this is a valid end-of-contents depends on
    /// where it is (X.690 §8.1.5).
    /// </summary>
    public readonly bool IsEndOfContentsOctets => IdentifierOctet == Ber.END_OF_CONTENTS_IDENTIFIER;
}

/// <summary>Reads the identifier octets and length octets of an encoding (X.690 §8.1.2, §8.1.3).</summary>
internal static class IdentifierAndLengthReader
{
    /// <summary>
    /// Reads the identifier and length octets at the source's position and leaves the source at the first contents
    /// octet. Fails with <see cref="ParseFailureKind.UnexpectedEndOfData"/> when there is not even an identifier octet.
    /// </summary>
    public static bool TryRead<TSource>(ref TSource source, out IdentifierAndLength encoding, out ParseFailure failure)
        where TSource : struct, IByteSource
    {
        encoding = new IdentifierAndLength { IdentifierStart = source.Position };
        failure = default;

        var identifierOctet = source.ReadByte();
        if (identifierOctet == -1)
        {
            failure = new ParseFailure(ParseFailureKind.UnexpectedEndOfData, encoding.IdentifierStart);
            return false;
        }
        encoding.IdentifierOctet = identifierOctet;
        encoding.TagNumber = identifierOctet & Ber.TAG_NUMBER_MASK;
        if (encoding.TagNumber == Ber.HIGH_TAG_NUMBER_FORM
            && !TryReadHighTagNumber(ref source, encoding.IdentifierStart, out encoding.TagNumber, out failure))
        {
            return false;
        }
        if (!TryReadLength(ref source, encoding.IdentifierStart, out encoding.ContentsLength, out encoding.IsShortestDefiniteForm, out failure))
        {
            return false;
        }

        encoding.ContentsStart = source.Position;
        return true;
    }

    /// <summary>
    /// Reads a tag number in the high-tag-number form (X.690 §8.1.2.4): the subsequent identifier octets hold it in
    /// base 128, most significant digit first, with bit 8 set on every octet but the last.
    /// </summary>
    private static bool TryReadHighTagNumber<TSource>(ref TSource source, long identifierStart, out int tagNumber, out ParseFailure failure)
        where TSource : struct, IByteSource
    {
        tagNumber = 0;
        failure = default;
        long number = 0;
        int octet;
        do
        {
            octet = source.ReadByte();
            if (octet == -1)
            {
                return Fail(ParseFailureKind.IdentifierTruncated, identifierStart, out failure);
            }
            // Bits 7–1 of the first subsequent octet must not all be zero (§8.1.2.4.2 c).
            if (number == 0 && octet == Ber.TAG_NUMBER_CONTINUATION_FLAG)
            {
                return Fail(ParseFailureKind.NonMinimalTagNumber, identifierStart, out failure);
            }

            number = (number << 7) | (uint)(octet & ~Ber.TAG_NUMBER_CONTINUATION_FLAG);
            if (number > int.MaxValue)
            {
                return Fail(ParseFailureKind.TagNumberTooLarge, identifierStart, out failure);
            }
        }
        while ((octet & Ber.TAG_NUMBER_CONTINUATION_FLAG) != 0);

        // Tag numbers 0 to 30 must use the single-octet form (§8.1.2.2).
        if (number < Ber.HIGH_TAG_NUMBER_FORM)
        {
            return Fail(ParseFailureKind.NonMinimalTagNumber, identifierStart, out failure);
        }

        tagNumber = (int)number;
        return true;
    }

    /// <summary>Reads the length octets (X.690 §8.1.3).</summary>
    /// <param name="source">The input, positioned at the first length octet.</param>
    /// <param name="identifierStart">Offset of the identifier octets, for error messages.</param>
    /// <param name="contentsLength">The number of contents octets, or <see langword="null"/> in the indefinite form.</param>
    /// <param name="isShortestDefiniteForm">The length is in the definite form with the fewest octets, as DER requires.</param>
    /// <param name="failure">Why reading failed.</param>
    private static bool TryReadLength<TSource>(ref TSource source, long identifierStart, out long? contentsLength, out bool isShortestDefiniteForm, out ParseFailure failure)
        where TSource : struct, IByteSource
    {
        contentsLength = null;
        isShortestDefiniteForm = false;
        failure = default;
        var first = source.ReadByte();
        if (first == -1)
        {
            return Fail(ParseFailureKind.LengthTruncated, identifierStart, out failure);
        }

        if ((first & Ber.LONG_FORM_FLAG) == 0)
        {
            contentsLength = first; // short form (§8.1.3.4): the octet is the length
            isShortestDefiniteForm = true;
            return true;
        }

        var octetCount = first & Ber.LENGTH_OCTET_COUNT_MASK;
        if (octetCount == 0)
        {
            return true; // indefinite form (§8.1.3.6)
        }

        if (octetCount > Ber.MAX_LENGTH_OCTETS)
        {
            failure = new ParseFailure(ParseFailureKind.TooManyLengthOctets, identifierStart, octetCount);
            return false;
        }

        // Long form (§8.1.3.5): the length in octetCount subsequent octets, most significant first.
        long length = 0;
        for (int i = 0; i < octetCount; i++)
        {
            var octet = source.ReadByte();
            if (octet == -1)
            {
                return Fail(ParseFailureKind.LengthTruncated, identifierStart, out failure);
            }

            if (i == 0)
            {
                isShortestDefiniteForm = octet != 0; // a leading zero octet means fewer octets would do
            }

            length = (length << 8) | (uint)octet;
        }
        isShortestDefiniteForm &= length >= 0x80; // shorter lengths must use the short form

        // Contents are read into a byte[], so lengths are limited to what an array can index.
        if (length > int.MaxValue)
        {
            return Fail(ParseFailureKind.LengthTooLarge, identifierStart, out failure);
        }

        contentsLength = length;
        return true;
    }

    private static bool Fail(ParseFailureKind kind, long offset, out ParseFailure failure)
    {
        failure = new ParseFailure(kind, offset);
        return false;
    }
}
