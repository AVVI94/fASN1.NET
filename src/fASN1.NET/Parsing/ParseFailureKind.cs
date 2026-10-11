namespace fASN1.NET.Parsing;

/// <summary>Why a parse failed. See <see cref="ParseFailure"/> for the message of each.</summary>
internal enum ParseFailureKind
{
    None,

    // Identifier octets (X.690 §8.1.2)
    UnexpectedEndOfData,
    IdentifierTruncated,
    NonMinimalTagNumber,
    TagNumberTooLarge,

    // Length octets (§8.1.3)
    LengthTruncated,
    TooManyLengthOctets,
    LengthTooLarge,
    IndefiniteFormInPrimitive,

    // Contents octets (§8.1.4) and end-of-contents octets (§8.1.5)
    ContentsPastEndOfData,
    ContentsReadIncomplete,
    NestedContentsPastEndOfData,
    ContentsLengthMismatch,
    MissingEndOfContents,
    InvalidEndOfContents,
    EndOfContentsOutsideIndefiniteForm,

    // Constructed strings (§8.6.4, §8.7.3, §8.23.6)
    InvalidFragment,
    InvalidBitStringFragment,

    // Encapsulation in OCTET STRING: the encodings inside must be DER (see StructureScanner.Encapsulation.cs)
    NonDerLengthInEncapsulation,
    ConstructedStringInEncapsulation,
    SetNotInDerOrderInEncapsulation,

    // The data: a byte array holds exactly one encoding
    OctetsAfterEncoding,
}
