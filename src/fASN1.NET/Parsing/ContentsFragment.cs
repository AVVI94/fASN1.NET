namespace fASN1.NET.Parsing;

/// <summary>
/// The contents octets of one primitive fragment of a constructed string; for a BIT STRING fragment, without its
/// unused-bits octet.
/// </summary>
internal struct ContentsFragment
{
    /// <summary>Offset of the first octet.</summary>
    public long Start;

    /// <summary>Number of octets.</summary>
    public long Length;
}
