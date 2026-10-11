using System.IO;

namespace fASN1.NET.Parsing;

/// <summary>
/// A seekable stream, read from its current position. The position lives in the stream, so copies of
/// this struct all see and move the same position.
/// </summary>
internal readonly struct StreamSource(Stream stream) : IByteSource
{
    private readonly long _length = stream.Length;

    public long Length => _length;

    public long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    public int ReadByte() => stream.ReadByte();

    public int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
}
