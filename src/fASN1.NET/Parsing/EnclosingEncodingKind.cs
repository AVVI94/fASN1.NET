namespace fASN1.NET.Parsing;

/// <summary>Why the contents of an encoding are read as nested encodings, which decides what a failure means.</summary>
internal enum EnclosingEncodingKind
{
    /// <summary>A constructed encoding. A nested encoding that is invalid makes it invalid too.</summary>
    Constructed,

    /// <summary>
    /// A primitive OCTET STRING whose contents are tried as encapsulated encodings, as X.509 nests DER in OCTET
    /// STRINGs. If they are not a complete run of DER encodings, the OCTET STRING is a leaf with its raw contents.
    /// </summary>
    EncapsulatingOctetString,

    /// <summary>
    /// A string in the constructed encoding (X.690 §8.6.4, §8.7.3, §8.23.6): its nested encodings are fragments whose
    /// contents are joined into one value.
    /// </summary>
    ConstructedString,

    /// <summary>A fragment of a constructed string that is itself constructed; its fragments belong to that string.</summary>
    ConstructedFragment,
}
