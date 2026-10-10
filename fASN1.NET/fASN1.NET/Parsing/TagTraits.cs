using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// What the scanner needs to know about the tag a factory creates for an identifier byte, so that it
/// can decide the tree's shape without creating tags.
/// </summary>
internal readonly struct TagTraits(bool isConstructed, bool isEncapsulationCandidate, string tagName)
{
    /// <summary>The content is read as a sequence of child elements.</summary>
    public bool IsConstructed { get; } = isConstructed;

    /// <summary>
    /// A primitive universal OCTET STRING. Its content is tried as child elements and kept as raw bytes
    /// if it does not parse as such.
    /// </summary>
    public bool IsEncapsulationCandidate { get; } = isEncapsulationCandidate;

    /// <summary>Name of the tag, used in error messages.</summary>
    public string TagName { get; } = tagName;

    /// <summary>The traits have been determined (the default value has not).</summary>
    public bool IsKnown => TagName is not null;

    /// <summary>Derives the traits from a tag freshly created for <paramref name="identifier"/>.</summary>
    public static TagTraits Of(ITag freshTag, int identifier)
    {
        var isConstructed = freshTag.IsConstructed || (identifier & Ber.CONSTRUCTED_FLAG) != 0;
        // A BIT STRING is never a candidate: signatures and keys would otherwise be expanded by accident.
        var isEncapsulationCandidate = !isConstructed
                                       && freshTag.IsUniversal
                                       && freshTag.TagNumber == (int)Tag.OctetString;
        return new TagTraits(isConstructed, isEncapsulationCandidate, freshTag.TagName);
    }
}
