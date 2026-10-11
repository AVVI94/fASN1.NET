namespace fASN1.NET.Parsing;

/// <summary>
/// A parse failure kept as data. The message is formatted only if the whole parse fails: a failed
/// encapsulation probe records a failure too, and the parse usually continues and succeeds.
/// </summary>
/// <param name="kind">What went wrong.</param>
/// <param name="offset">Offset in the input that the message refers to.</param>
/// <param name="value">A number the message mentions, such as a declared length.</param>
/// <param name="tagName">The tag or text the message mentions, if any.</param>
/// <param name="identifier">
/// First identifier byte of the tag the message names, or -1. Its name is asked of the factory only when the message
/// is formatted, so the scan creates no tags.
/// </param>
/// <param name="tagNumber">Tag number of the tag the message names.</param>
internal readonly struct ParseFailure(ParseFailureKind kind, long offset, long value = 0, string? tagName = null, int identifier = -1, int tagNumber = 0)
{
    /// <summary>What went wrong.</summary>
    public ParseFailureKind Kind => kind;

    /// <summary>Formats the message, naming tags as <paramref name="factory"/> names them.</summary>
    public string ToMessage(Factory.ITagFactory factory)
    {
        var name = tagName ?? (identifier >= 0 ? Ber.CreateTag(factory, identifier, tagNumber).TagName : null);
        return Format(name);
    }

    private string Format(string? tagName) => kind switch
    {
        ParseFailureKind.UnexpectedEndOfData => $"Unexpected end of data at offset {offset}",
        ParseFailureKind.MissingEndOfContents => $"Indefinite-length container at offset {offset} has no end-of-contents marker",
        ParseFailureKind.IdentifierTruncated => $"Unexpected end of data in the identifier of the element at offset {offset}",
        ParseFailureKind.NonMinimalTagNumber => $"The tag number of the element at offset {offset} is not in the shortest form",
        ParseFailureKind.TagNumberTooLarge => $"The tag number of the element at offset {offset} is too large",
        ParseFailureKind.InvalidEndOfContents => $"Invalid end-of-contents marker at offset {offset}",
        ParseFailureKind.ContainerPastEndOfData => $"Container at offset {offset} has a length of {value}, which is past the end of the stream",
        ParseFailureKind.ContainerSizeMismatch => $"Content size is not correct for container at offset {offset}",
        ParseFailureKind.IndefiniteLengthPrimitive => "Cannot skip over an invalid tag with indefinite length",
        ParseFailureKind.ContentPastEndOfData => $"Content size is not correct for tag {tagName} at offset {offset}",
        ParseFailureKind.ContentReadIncomplete => $"Unexpected end of data in the content of tag {tagName} at offset {offset}",
        ParseFailureKind.LengthTruncated => $"Unexpected end of data in the length of the element at offset {offset}",
        ParseFailureKind.TooManyLengthOctets => $"Length of the element at offset {offset} uses {value} octets; at most {Ber.MAX_LENGTH_OCTETS} are supported",
        ParseFailureKind.LengthTooLarge => $"Length of the element at offset {offset} is too large",
        ParseFailureKind.NonDerLengthInOctetString => $"Length of the element at offset {offset} is not in DER form, so the OCTET STRING holding it is kept as raw bytes",
        ParseFailureKind.SetNotInDerOrderInOctetString => $"The element at offset {offset} is out of DER order in its SET, so the OCTET STRING holding it is kept as raw bytes",
        ParseFailureKind.EndOfContentsInOctetString => $"End-of-contents marker at offset {offset} cannot be a value, so the OCTET STRING holding it is kept as raw bytes",
        ParseFailureKind.InvalidStringSegment => $"Segment at offset {offset} of a constructed string is not {tagName}",
        ParseFailureKind.InvalidBitStringSegment => $"BIT STRING segment at offset {offset} has an invalid unused-bits count; only the last segment may have unused bits",
        ParseFailureKind.ConstructedStringInOctetString => $"The element at offset {offset} is a constructed string, which DER does not allow, so the OCTET STRING holding it is kept as raw bytes",
        _ => "Unknown deserialization error",
    };
}
