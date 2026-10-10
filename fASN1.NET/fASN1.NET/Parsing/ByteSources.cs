using System;
using System.IO;

namespace fASN1.NET.Parsing;

/// <summary>
/// The input of a parse. Implemented by structs and used through generic parameters, so the JIT emits
/// a specialized parser per source with direct calls and nothing to allocate.
/// </summary>
internal interface IByteSource
{
    long Length { get; }
    long Position { get; set; }
    int ReadByte();
    int Read(byte[] buffer, int offset, int count);
}

/// <summary>A byte array read from offset 0.</summary>
internal struct ArrayByteSource(byte[] data) : IByteSource
{
    private int _position;

    public readonly long Length => data.Length;

    public long Position
    {
        readonly get => _position;
        set => _position = (int)value;
    }

    public int ReadByte() => _position < data.Length ? data[_position++] : -1;

    public int Read(byte[] buffer, int offset, int count)
    {
        var n = Math.Min(count, data.Length - _position);
        if (n <= 0)
            return 0;
        Buffer.BlockCopy(data, _position, buffer, offset, n);
        _position += n;
        return n;
    }
}

/// <summary>A seekable stream, read from its current position.</summary>
internal readonly struct StreamByteSource(Stream stream) : IByteSource
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
