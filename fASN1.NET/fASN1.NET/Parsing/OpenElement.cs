namespace fASN1.NET.Parsing;

/// <summary>An element whose child elements the scanner is currently reading.</summary>
internal struct OpenElement
{
    public OpenElementKind Kind;

    /// <summary>Index of the element's <see cref="ScannedNode"/>.</summary>
    public int NodeIndex;

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
