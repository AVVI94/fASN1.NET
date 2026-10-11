namespace fASN1.NET.Parsing;

/// <summary>
/// One node of the final tree, as found by <see cref="StructureScanner{TSource}"/>: one encoding, or for a constructed
/// string the whole string. Nodes are stored in pre-order, so the children of a node are the
/// <see cref="ChildCount"/> subtrees that follow it.
/// </summary>
internal struct ScannedNode
{
    /// <summary>
    /// The first identifier octet: class, primitive or constructed, and the tag number when it is below 31. For a
    /// constructed string it is the primitive form, so the factory creates the same tag as for a primitive string.
    /// </summary>
    public int IdentifierOctet;

    /// <summary>The tag number, also when it is in the high-tag-number form.</summary>
    public int TagNumber;

    /// <summary>Offset of the first contents octet.</summary>
    public long ContentsStart;

    /// <summary>Number of contents octets of a leaf; for a constructed string, the joined length.</summary>
    public long ContentsLength;

    /// <summary>The length is in the indefinite form.</summary>
    public bool IsIndefiniteForm;

    /// <summary>
    /// The contents were read as nested encodings (possibly none), which become the node's children: a constructed
    /// encoding, or an OCTET STRING encapsulating encodings.
    /// </summary>
    public bool ContentsAreChildren;

    /// <summary>Number of children when <see cref="ContentsAreChildren"/> is set.</summary>
    public int ChildCount;

    /// <summary>
    /// A string in the constructed encoding (X.690 §8.6.4, §8.7.3, §8.23.6): its value is the contents of its
    /// <see cref="FragmentCount"/> primitive fragments from <see cref="FirstFragment"/>, after the unused-bits octet
    /// <see cref="UnusedBits"/> for a BIT STRING, and <see cref="ContentsLength"/> is their total.
    /// </summary>
    public bool IsConstructedString;

    /// <summary>Index of the first fragment of a constructed string.</summary>
    public int FirstFragment;

    /// <summary>Number of fragments of a constructed string.</summary>
    public int FragmentCount;

    /// <summary>Unused bits in the final octet of a constructed BIT STRING, from its last fragment.</summary>
    public int UnusedBits;
}
