namespace fASN1.NET.Parsing;

/// <summary>The identifier and length of an element, as read from the input.</summary>
internal struct ElementHeader
{
    /// <summary>Offset of the first identifier byte.</summary>
    public long Start;

    /// <summary>The first identifier byte: class, constructed bit, and the tag number when it is below 31.</summary>
    public int Identifier;

    /// <summary>The tag number, also when it took several identifier bytes.</summary>
    public int TagNumber;

    /// <summary>Offset of the first content byte.</summary>
    public long ContentStart;

    /// <summary>Declared content length; <see langword="null"/> for indefinite length.</summary>
    public long? ContentLength;

    /// <summary>The length is definite and in the shortest form, as DER requires.</summary>
    public bool HasDerLength;

    /// <summary>The content ends at an end-of-contents marker rather than after a declared length.</summary>
    public readonly bool IsIndefiniteLength => ContentLength is null;

    /// <summary>Offset just past the content of a definite-length element.</summary>
    public readonly long ContentEnd => ContentStart + ContentLength!.Value;

    /// <summary>The element is an end-of-contents marker (or something with its identifier, in the wrong place).</summary>
    public readonly bool IsEndOfContents => Identifier == Ber.END_OF_CONTENTS;
}

/// <summary>Reads the identifier and length of an element (X.690 §8.1.2, §8.1.3).</summary>
internal static class ElementHeaderReader
{
    /// <summary>
    /// Reads the header at the source's position and leaves the source at the first content byte. Fails with
    /// <see cref="ParseFailureKind.UnexpectedEndOfData"/> when there is not even an identifier byte.
    /// </summary>
    public static bool TryRead<TSource>(ref TSource source, out ElementHeader header, out ParseFailure failure)
        where TSource : struct, IByteSource
    {
        header = new ElementHeader { Start = source.Position };
        failure = default;

        var identifier = source.ReadByte();
        if (identifier == -1)
        {
            failure = new ParseFailure(ParseFailureKind.UnexpectedEndOfData, header.Start);
            return false;
        }
        header.Identifier = identifier;
        header.TagNumber = identifier & Ber.TAG_NUMBER_MASK;
        if (header.TagNumber == Ber.TAG_NUMBER_MASK && !TryReadMultiByteTagNumber(ref source, header.Start, out header.TagNumber, out failure))
            return false;
        if (!TryReadLength(ref source, header.Start, out header.ContentLength, out header.HasDerLength, out failure))
            return false;

        header.ContentStart = source.Position;
        return true;
    }

    /// <summary>
    /// Reads a tag number of 31 or more, which follows the first identifier byte in base 128, most significant digit
    /// first, with the top bit set on every byte but the last (X.690 §8.1.2.4).
    /// </summary>
    private static bool TryReadMultiByteTagNumber<TSource>(ref TSource source, long elementStart, out int tagNumber, out ParseFailure failure)
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
                return Fail(ParseFailureKind.IdentifierTruncated, elementStart, out failure);
            // A leading 0x80 only adds a zero digit (§8.1.2.4.2 c).
            if (number == 0 && octet == Ber.LONG_FORM_LENGTH_FLAG)
                return Fail(ParseFailureKind.NonMinimalTagNumber, elementStart, out failure);
            number = (number << 7) | (uint)(octet & ~Ber.LONG_FORM_LENGTH_FLAG);
            if (number > int.MaxValue)
                return Fail(ParseFailureKind.TagNumberTooLarge, elementStart, out failure);
        }
        while ((octet & Ber.LONG_FORM_LENGTH_FLAG) != 0);

        // Numbers up to 30 must use the single-byte form (§8.1.2.2).
        if (number < Ber.TAG_NUMBER_MASK)
            return Fail(ParseFailureKind.NonMinimalTagNumber, elementStart, out failure);
        tagNumber = (int)number;
        return true;
    }

    /// <summary>Reads the length bytes that follow an identifier.</summary>
    /// <param name="source">The input, positioned at the first length byte.</param>
    /// <param name="elementStart">Offset of the identifier, for error messages.</param>
    /// <param name="contentLength">The content length, or <see langword="null"/> for indefinite length.</param>
    /// <param name="isDer">The length is definite and in the shortest form, as DER requires.</param>
    /// <param name="failure">Why reading failed.</param>
    private static bool TryReadLength<TSource>(ref TSource source, long elementStart, out long? contentLength, out bool isDer, out ParseFailure failure)
        where TSource : struct, IByteSource
    {
        contentLength = null;
        isDer = false;
        failure = default;
        var first = source.ReadByte();
        if (first == -1)
            return Fail(ParseFailureKind.LengthTruncated, elementStart, out failure);

        if ((first & Ber.LONG_FORM_LENGTH_FLAG) == 0)
        {
            contentLength = first; // short form: the byte is the length
            isDer = true;
            return true;
        }

        var octetCount = first & Ber.LENGTH_OCTET_COUNT_MASK;
        if (octetCount == 0)
            return true; // indefinite length
        if (octetCount > Ber.MAX_LENGTH_OCTETS)
        {
            failure = new ParseFailure(ParseFailureKind.TooManyLengthOctets, elementStart, octetCount);
            return false;
        }

        long length = 0;
        for (int i = 0; i < octetCount; i++)
        {
            var octet = source.ReadByte();
            if (octet == -1)
                return Fail(ParseFailureKind.LengthTruncated, elementStart, out failure);
            if (i == 0)
                isDer = octet != 0; // a leading zero octet means fewer octets would do
            length = (length << 8) | (uint)octet;
        }
        isDer &= length >= 0x80; // shorter lengths must use the short form

        // Content is read into a byte[], so lengths are limited to what an array can index.
        if (length > int.MaxValue)
            return Fail(ParseFailureKind.LengthTooLarge, elementStart, out failure);

        contentLength = length;
        return true;
    }

    private static bool Fail(ParseFailureKind kind, long offset, out ParseFailure failure)
    {
        failure = new ParseFailure(kind, offset);
        return false;
    }
}
