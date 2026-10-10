namespace fASN1.NET.Parsing;

/// <summary>Why a parse failed. See <see cref="ParseFailure.ToMessage"/> for the text of each.</summary>
internal enum ParseFailureKind
{
    None,
    UnexpectedEndOfData,
    MissingEndOfContents,
    IdentifierTruncated,
    NonMinimalTagNumber,
    TagNumberTooLarge,
    InvalidEndOfContents,
    ContainerPastEndOfData,
    ContainerSizeMismatch,
    IndefiniteLengthPrimitive,
    ContentPastEndOfData,
    ContentReadIncomplete,
    LengthTruncated,
    TooManyLengthOctets,
    LengthTooLarge,
    NonDerLengthInOctetString,
    SetNotInDerOrderInOctetString,
}
