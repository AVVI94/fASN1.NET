namespace fASN1.NET.Parsing;

/// <summary>
/// The input of a parse. Implemented by structs and used as a generic type argument, so the JIT emits a
/// parser specialized for each input type, with direct calls and nothing to allocate.
/// </summary>
internal interface IByteSource
{
    /// <summary>Total length of the input.</summary>
    long Length { get; }

    /// <summary>Offset of the next byte to read.</summary>
    long Position { get; set; }

    /// <summary>Reads one byte, or returns -1 at the end of the input.</summary>
    int ReadByte();

    /// <summary>Reads up to <paramref name="count"/> bytes and returns how many were read; 0 at the end of the input.</summary>
    int Read(byte[] buffer, int offset, int count);
}
