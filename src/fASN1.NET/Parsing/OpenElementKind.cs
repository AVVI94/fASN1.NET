namespace fASN1.NET.Parsing;

/// <summary>Why an element's content is being read as nested elements, which decides what a failure means.</summary>
internal enum OpenElementKind
{
    /// <summary>A constructed element. A child that fails to parse makes the element fail.</summary>
    Constructed,

    /// <summary>
    /// A primitive OCTET STRING whose content is tried as nested elements. A child that fails to parse rolls the
    /// element back to a leaf holding the raw bytes.
    /// </summary>
    EncapsulationProbe,

    /// <summary>A constructed string: its children are segments whose contents are joined into one value.</summary>
    ConstructedString,

    /// <summary>A constructed segment of a constructed string; its segments belong to that string.</summary>
    StringSegment,
}
