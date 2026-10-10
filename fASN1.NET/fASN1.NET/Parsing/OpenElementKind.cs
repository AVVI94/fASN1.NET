namespace fASN1.NET.Parsing;

/// <summary>Why an element's content is being read as child elements.</summary>
internal enum OpenElementKind
{
    /// <summary>A constructed element. A child that fails to parse makes the element fail.</summary>
    Constructed,

    /// <summary>
    /// A primitive OCTET STRING whose content is tried as child elements. A child that fails to parse
    /// rolls the element back to a leaf holding the raw bytes.
    /// </summary>
    EncapsulationProbe,
}
