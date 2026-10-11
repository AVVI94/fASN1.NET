namespace fASN1.NET.Parsing;

/// <summary>A run of content bytes in the input: one segment of a constructed string.</summary>
internal struct ContentSegment
{
    /// <summary>Offset of the first byte.</summary>
    public long Start;

    /// <summary>Number of bytes.</summary>
    public long Length;
}
