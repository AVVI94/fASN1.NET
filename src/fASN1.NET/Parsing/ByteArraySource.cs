using System;

namespace fASN1.NET.Parsing;

/// <summary>A byte array, read from offset 0.</summary>
internal struct ByteArraySource(byte[] data) : IByteSource
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
        var available = Math.Min(count, data.Length - _position);
        if (available <= 0)
        {
            return 0;
        }

        Buffer.BlockCopy(data, _position, buffer, offset, available);
        _position += available;
        return available;
    }
}
