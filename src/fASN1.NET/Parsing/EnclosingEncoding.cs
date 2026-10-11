namespace fASN1.NET.Parsing;

/// <summary>An encoding whose contents the scanner is reading as nested encodings.</summary>
internal struct EnclosingEncoding
{
    public EnclosingEncodingKind Kind;

    /// <summary>The encoding's identifier and length octets.</summary>
    public IdentifierAndLength IdentifierAndLength;

    /// <summary>Index of the encoding's <see cref="ScannedNode"/>; for a constructed fragment, the node of its string.</summary>
    public int NodeIndex;

    /// <summary>
    /// Offset that no nested encoding may cross: the end of the contents in the definite form, otherwise the bound of
    /// the enclosing encoding. Bounding every encoding by its innermost definite-form ancestor keeps an encapsulating
    /// OCTET STRING whose contents turn out not to be encodings from reading past its own contents.
    /// </summary>
    public long ContentsBound;

    /// <summary>Contents start of the definite-form encoding that sets <see cref="ContentsBound"/>, for error messages.</summary>
    public long ContentsBoundStart;

    /// <summary>
    /// The encoding is an encapsulating OCTET STRING or lies inside one, so everything inside it must be DER in the
    /// respects <see cref="Asn1Serializer.Serialize"/> normalizes; see <c>StructureScanner.CheckDerForm</c>.
    /// </summary>
    public bool MustBeDer;

    /// <summary>Number of nested encodings read so far.</summary>
    public int NestedEncodingCount;

    /// <summary>
    /// Offset of the nested encoding being read, or of the last one once it is complete. Nested encodings lie side by
    /// side, so the previous one ends where this one starts.
    /// </summary>
    public long NestedEncodingStart;

    /// <summary>
    /// Offset of the nested encoding before <see cref="NestedEncodingStart"/>, to check the order of a SET's
    /// components.
    /// </summary>
    public long PreviousNestedEncodingStart;

    /// <summary>For a constructed string or a constructed fragment: the tag number its fragments must have.</summary>
    public int FragmentTagNumber;

    public readonly bool IsIndefiniteForm => IdentifierAndLength.IsIndefiniteForm;

    public readonly long ContentsStart => IdentifierAndLength.ContentsStart;

    public readonly long ContentsEnd => IdentifierAndLength.ContentsEnd;
}
