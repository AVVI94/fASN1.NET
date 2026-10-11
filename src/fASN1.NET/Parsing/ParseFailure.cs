namespace fASN1.NET.Parsing;

/// <summary>
/// A parse failure kept as data. The message is formatted only if the whole parse fails: an OCTET STRING whose
/// contents turn out not to be encodings records a failure too, and the parse continues and usually succeeds.
/// </summary>
/// <param name="kind">What went wrong.</param>
/// <param name="offset">Offset in the input that the message refers to.</param>
/// <param name="value">A number the message mentions, such as a declared length.</param>
/// <param name="tagName">The tag or text the message mentions, if any.</param>
/// <param name="identifierOctet">
/// First identifier octet of the tag the message names, or -1. Its name is asked of the factory only when the message
/// is formatted, so the scan creates no tags.
/// </param>
/// <param name="tagNumber">Tag number of the tag the message names.</param>
internal readonly struct ParseFailure(ParseFailureKind kind, long offset, long value = 0, string? tagName = null, int identifierOctet = -1, int tagNumber = 0)
{
    /// <summary>What went wrong.</summary>
    public ParseFailureKind Kind => kind;

    /// <summary>Formats the message, naming tags as <paramref name="factory"/> names them.</summary>
    public string ToMessage(Factory.ITagFactory factory)
    {
        var name = tagName ?? (identifierOctet >= 0 ? Ber.CreateTag(factory, identifierOctet, tagNumber).TagName : null);
        return Format(name);
    }

    private string Format(string? tagName) => kind switch
    {
        ParseFailureKind.UnexpectedEndOfData => $"Unexpected end of data at offset {offset}",
        ParseFailureKind.IdentifierTruncated => $"Unexpected end of data in the identifier octets of the encoding at offset {offset}",
        ParseFailureKind.NonMinimalTagNumber => $"The tag number of the encoding at offset {offset} is not in the shortest form",
        ParseFailureKind.TagNumberTooLarge => $"The tag number of the encoding at offset {offset} is too large",

        ParseFailureKind.LengthTruncated => $"Unexpected end of data in the length octets of the encoding at offset {offset}",
        ParseFailureKind.TooManyLengthOctets => $"The length of the encoding at offset {offset} uses {value} octets; at most {Ber.MAX_LENGTH_OCTETS} are supported",
        ParseFailureKind.LengthTooLarge => $"The length of the encoding at offset {offset} is too large",
        ParseFailureKind.IndefiniteFormInPrimitive => $"The primitive encoding with contents at offset {offset} has a length in the indefinite form",

        ParseFailureKind.ContentsPastEndOfData => $"The contents of {tagName} at offset {offset} reach past the end of the data",
        ParseFailureKind.ContentsReadIncomplete => $"Unexpected end of data in the contents of {tagName} at offset {offset}",
        ParseFailureKind.NestedContentsPastEndOfData => $"The contents at offset {offset} have a length of {value}, which reaches past the end of the data",
        ParseFailureKind.ContentsLengthMismatch => $"The nested encodings do not match the length of the contents at offset {offset}",
        ParseFailureKind.MissingEndOfContents => $"The contents in the indefinite form at offset {offset} have no end-of-contents octets",
        ParseFailureKind.InvalidEndOfContents => $"Invalid end-of-contents octets at offset {offset}",

        ParseFailureKind.InvalidFragment => $"The fragment at offset {offset} of a constructed string is not {tagName}",
        ParseFailureKind.InvalidBitStringFragment => $"The BIT STRING fragment at offset {offset} has an invalid number of unused bits; only the last fragment may have unused bits",

        ParseFailureKind.NonDerLengthInEncapsulation => $"The length of the encoding at offset {offset} is not DER, so the OCTET STRING does not encapsulate it",
        ParseFailureKind.EndOfContentsInEncapsulation => $"End-of-contents octets at offset {offset} are not a value, so the OCTET STRING does not encapsulate encodings",
        ParseFailureKind.ConstructedStringInEncapsulation => $"The encoding at offset {offset} is a constructed string, which is not DER, so the OCTET STRING does not encapsulate it",
        ParseFailureKind.SetNotInDerOrderInEncapsulation => $"The SET component at offset {offset} is not in DER order, so the OCTET STRING does not encapsulate it",
        _ => "Unknown deserialization error",
    };
}
