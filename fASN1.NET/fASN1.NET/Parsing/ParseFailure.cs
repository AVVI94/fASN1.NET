namespace fASN1.NET.Parsing;

/// <summary>
/// A parse failure kept as data. The message is formatted only if the whole parse fails: a failed
/// encapsulation probe records a failure too, and the parse usually continues and succeeds.
/// </summary>
/// <param name="kind">What went wrong.</param>
/// <param name="offset">Offset in the input that the message refers to.</param>
/// <param name="value">A number the message mentions, such as a declared length.</param>
/// <param name="tagName">The tag the message mentions, if any.</param>
internal readonly struct ParseFailure(ParseFailureKind kind, long offset, long value = 0, string? tagName = null)
{
    public string ToMessage() => kind switch
    {
        ParseFailureKind.UnexpectedEndOfData => $"Unexpected end of data at offset {offset}",
        ParseFailureKind.MissingEndOfContents => $"Indefinite-length container at offset {offset} has no end-of-contents marker",
        ParseFailureKind.MultiByteTagNumber => $"Element at offset {offset} uses a multi-byte tag number, which is not supported",
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
        _ => "Unknown deserialization error",
    };
}
