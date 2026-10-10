using fASN1.NET.Tags;

namespace fASN1.NET.Parsing;

/// <summary>
/// What the scanner needs to know about the tag a factory creates for an identifier byte, so that it
/// can decide the tree's shape without creating tags.
/// </summary>
internal readonly struct TagTraits(bool isConstructed, bool isEncapsulationCandidate, int stringSegmentTagNumber, string tagName)
{
    /// <summary>The content is read as a sequence of child elements.</summary>
    public bool IsConstructed { get; } = isConstructed;

    /// <summary>
    /// A primitive universal OCTET STRING. Its content is tried as child elements and kept as raw bytes
    /// if it does not parse as such.
    /// </summary>
    public bool IsEncapsulationCandidate { get; } = isEncapsulationCandidate;

    /// <summary>
    /// For a constructed string (BIT STRING, OCTET STRING, a character string or a time) the tag number of its
    /// segments: 3 for a BIT STRING, 4 for the others (X.690 §8.6.4, §8.7.3, §8.23.6). 0 for any other element.
    /// </summary>
    public int StringSegmentTagNumber { get; } = stringSegmentTagNumber;

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
        return new TagTraits(isConstructed, isEncapsulationCandidate, StringSegmentTagNumberOf(identifier), freshTag.TagName);
    }

    /// <summary>The segment tag number of a constructed universal string identifier, or 0.</summary>
    private static int StringSegmentTagNumberOf(int identifier)
    {
        if ((identifier & ~Ber.TAG_NUMBER_MASK) != Ber.CONSTRUCTED_FLAG)
            return 0; // not universal and constructed
        return (identifier & Ber.TAG_NUMBER_MASK) switch
        {
            (int)Tag.BitString => (int)Tag.BitString,
            (int)Tag.OctetString or (int)Tag.ObjectDescriptor or (int)Tag.UTF8String
                or (>= (int)Tag.NumericString and <= (int)Tag.UniversalString) or (int)Tag.BMPString => (int)Tag.OctetString,
            _ => 0,
        };
    }
}
