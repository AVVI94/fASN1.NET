namespace fASN1.NET.Parsing;

/// <summary>An element whose content the scanner is reading as nested elements.</summary>
internal struct OpenElement
{
    public OpenElementKind Kind;

    /// <summary>The element's identifier and length.</summary>
    public ElementHeader Header;

    /// <summary>Index of the element's <see cref="ScannedNode"/>; for a string segment, the node of its string.</summary>
    public int NodeIndex;

    /// <summary>
    /// Offset that nothing inside this element may cross: its own content end when it has a definite length,
    /// otherwise the limit of the element that contains it. Bounding every element by its innermost
    /// definite-length ancestor keeps a failed probe from scanning past its own content.
    /// </summary>
    public long Limit;

    /// <summary>Content start of the definite-length element that sets <see cref="Limit"/>, for error messages.</summary>
    public long LimitOwnerStart;

    /// <summary>
    /// The element is an encapsulation probe or lies inside one, so everything inside it must be in the form
    /// <see cref="Asn1Serializer.Serialize"/> writes back unchanged; see <c>StructureScanner.CheckDerForm</c>.
    /// </summary>
    public bool RequiresDer;

    /// <summary>Number of direct children read so far.</summary>
    public int ChildCount;

    /// <summary>
    /// Offset of the direct child being read, or of the last one once it is complete. The children of an element
    /// lie side by side, so the previous child ends where this one starts.
    /// </summary>
    public long ChildStart;

    /// <summary>Offset of the direct child before <see cref="ChildStart"/>, to check the order of a SET's children.</summary>
    public long PreviousChildStart;

    /// <summary>For a constructed string or one of its segments: the tag number its segments must have.</summary>
    public int SegmentTagNumber;

    public readonly bool IsIndefiniteLength => Header.IsIndefiniteLength;

    public readonly long ContentStart => Header.ContentStart;

    public readonly long ContentEnd => Header.ContentEnd;
}
