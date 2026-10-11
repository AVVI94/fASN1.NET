namespace fASN1.NET.Parsing;

/// <summary>The outcome of scanning one element, or of closing an open element.</summary>
internal enum ElementScanResult
{
    /// <summary>The element is complete and valid.</summary>
    Succeeded,

    /// <summary>The element is invalid; the scanner's failure says why.</summary>
    Failed,

    /// <summary>The element's content is being read as child elements; they come next.</summary>
    ChildrenOpened,

    /// <summary>The end-of-contents marker of the innermost indefinite-length element was read.</summary>
    EndOfContentsRead,
}
