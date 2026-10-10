namespace fASN1.NET.Parsing;

/// <summary>
/// One node of the final tree, as found by <see cref="StructureScanner{TSource}"/>. Nodes are stored in
/// pre-order, so the children of a node are the <see cref="ChildCount"/> subtrees that follow it.
/// </summary>
internal struct ScannedNode
{
    /// <summary>The first identifier byte: class, constructed bit, and the tag number when it is below 31.</summary>
    public int Identifier;

    /// <summary>The tag number, also when it took several identifier bytes.</summary>
    public int TagNumber;

    /// <summary>Offset of the first content byte.</summary>
    public long ContentStart;

    /// <summary>Content length; meaningful for leaves.</summary>
    public long ContentLength;

    public bool IsIndefiniteLength;

    /// <summary>The content was read as child elements (possibly none), not as bytes.</summary>
    public bool ContentIsChildren;

    /// <summary>Number of direct children when <see cref="ContentIsChildren"/> is set.</summary>
    public int ChildCount;

    /// <summary>
    /// An OCTET STRING whose content was tried as child elements and did not parse as such; it is a leaf
    /// holding the raw bytes.
    /// </summary>
    public bool IsRolledBackProbe;
}
