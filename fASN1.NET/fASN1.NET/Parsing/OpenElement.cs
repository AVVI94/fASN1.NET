namespace fASN1.NET.Parsing;

/// <summary>An element whose child elements the scanner is currently reading.</summary>
internal struct OpenElement
{
    public OpenElementKind Kind;

    /// <summary>Index of the element's <see cref="ScannedNode"/>.</summary>
    public int NodeIndex;

    /// <summary>Offset of the identifier.</summary>
    public long ElementStart;

    /// <summary>Offset of the first content byte.</summary>
    public long ContentStart;

    /// <summary>Declared content length; <see langword="null"/> for indefinite length.</summary>
    public long? ContentLength;

    /// <summary>
    /// Offset that nothing inside this element may cross: its own content end when it has a definite length,
    /// otherwise the limit of the element that contains it. Bounding every element by its innermost
    /// definite-length ancestor keeps a failed probe from scanning past its own content.
    /// </summary>
    public long Limit;

    /// <summary>Content start of the definite-length element that sets <see cref="Limit"/>, for error messages.</summary>
    public long LimitOwnerStart;

    /// <summary>
    /// The element is an encapsulation probe or lies inside one, so every length inside it must be in DER form
    /// (definite, minimal). <see cref="Asn1Serializer.Serialize"/> writes DER lengths, so encapsulated children
    /// in any other form would re-encode to different bytes and change the OCTET STRING's value.
    /// </summary>
    public bool RequiresDerLengths;

    /// <summary>
    /// Extent of the last direct child read, for a SET that <see cref="RequiresDerLengths"/>: its children must be in
    /// DER order too, or Serialize would reorder them and change the OCTET STRING's value.
    /// </summary>
    public long PreviousChildStart, PreviousChildEnd;

    /// <summary>Number of direct children read so far.</summary>
    public int ChildCount;

    /// <summary>
    /// An end-of-contents marker appeared among the direct children of a probe. Such a marker cannot be
    /// content, so the probe will be rolled back.
    /// </summary>
    public bool HasEndOfContentsChild;

    /// <summary>The children end at an end-of-contents marker rather than at <see cref="ContentEnd"/>.</summary>
    public readonly bool IsIndefiniteLength => ContentLength is null;

    /// <summary>Offset just past the content of a definite-length element.</summary>
    public readonly long ContentEnd => ContentStart + ContentLength!.Value;
}
