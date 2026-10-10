namespace fASN1.NET.Parsing;

internal enum ErrorKind
{
    None,
    UnexpectedEnd,
    MissingEndOfContents,
    MultiByteTag,
    InvalidEndOfContents,
    ContainerPastEnd,
    ContainerSizeMismatch,
    IndefinitePrimitive,
    ContentPastEnd,
    ContentReadShort,
    LengthTruncated,
    TooManyLengthOctets,
    LengthTooLarge,
}

/// <summary>
/// A parse failure, kept as data and formatted only if the whole parse fails. Failed encapsulation
/// probes record failures too, and most parses that see them still succeed.
/// </summary>
internal readonly struct ParseError(ErrorKind kind, long offset, long value = 0, string? name = null)
{
    public string? Format() => kind switch
    {
        ErrorKind.None => null,
        ErrorKind.UnexpectedEnd => $"Unexpected end of data at offset {offset}",
        ErrorKind.MissingEndOfContents => $"Indefinite-length container at offset {offset} has no end-of-contents marker",
        ErrorKind.MultiByteTag => $"Element at offset {offset} uses a multi-byte tag number, which is not supported",
        ErrorKind.InvalidEndOfContents => $"Invalid end-of-contents marker at offset {offset}",
        ErrorKind.ContainerPastEnd => $"Container at offset {offset} has a length of {value}, which is past the end of the stream",
        ErrorKind.ContainerSizeMismatch => $"Content size is not correct for container at offset {offset}",
        ErrorKind.IndefinitePrimitive => "Cannot skip over an invalid tag with indefinite length",
        ErrorKind.ContentPastEnd => $"Content size is not correct for tag {name} at offset {offset}",
        ErrorKind.ContentReadShort => $"Unexpected end of data in the content of tag {name} at offset {offset}",
        ErrorKind.LengthTruncated => $"Unexpected end of data in the length of the element at offset {offset}",
        ErrorKind.TooManyLengthOctets => $"Length of the element at offset {offset} uses {value} octets; at most 6 are supported",
        ErrorKind.LengthTooLarge => $"Length of the element at offset {offset} is too large",
        _ => $"Invalid data at offset {offset}",
    };
}
